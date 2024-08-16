using Dapper;
using IdGen;
using MySqlConnector;
using System.Text;
using TechBlogCore.AOT.DtoParams;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Providers;
using TechBlogCore.RestApi.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class ArticleService
    {
        private readonly MySqlConnection conn;
        private readonly IdGenerator idGen;
        private readonly ILogger<ArticleService> logger;
        private readonly ICurrUserProvider currProvider;

        public ArticleService(MySqlConnection conn, IdGenerator idGen, ILogger<ArticleService> logger, ICurrUserProvider currProvider)
        {
            this.conn = conn;
            this.idGen = idGen;
            this.logger = logger;
            this.currProvider = currProvider;
        }

        [DapperAot]
        public async Task<PagedList<ArticleListDto>> GetArticles(ArticleDtoParam param)
        {
            var cond = "";
            if (param.IncludeDeleted == false)
            {
                cond += " AND a.IsDeleted = 0";
            }
            if (!param.Category.IsNullOrWhiteSpace())
            {
                cond += " AND b.Name = @Category";
            }
            if (!param.Tag.IsNullOrWhiteSpace())
            {
                cond += " AND a.Id in (SELECT Article_Id FROM blog_articletags a JOIN blog_tags b ON a.Tag_Id=b.Id WHERE b.Name = @Tag)";
            }
            if (!param.Keyword.IsNullOrWhiteSpace())
            {
                cond += " AND a.Title LIKE @Keyword";
            }

            var query_sql = $@"SELECT LOWER(TRIM(TRAILING '0' FROM HEX(a.Id))) AS Id
, Title
, CONCAT(
SUBSTRING(Content, 1, 260)
, CASE
    WHEN LENGTH(Content) > 260 THEN '…'
    ELSE ''
END) Content
, b.Name Category
, ViewsCount ViewCount
,(SELECT count(*) FROM blog_comments c WHERE c.Article_Id=a.Id AND c.IsDeleted = 0) CommentCount
, a.CreateTime
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.Category_Id
WHERE 1=1 {cond}
ORDER BY a.CreateTime desc LIMIT {(param.PageNumber-1)*param.PageSize}, {param.PageSize}";
            var count_sql = $@"SELECT count(*)
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.Category_Id
WHERE 1=1 {cond}";

            return new PagedList<ArticleListDto>
            (
                (await conn.QueryAsync<ArticleListDto>(query_sql, param)).AsList(),
                await conn.ExecuteScalarAsync<int>(count_sql, param),
                param.PageNumber,
                param.PageSize
            );
        }

        [DapperAot]
        public async Task<ArticleDetailDto> GetArticle(string hexId)
        {
            var id = hexId.HexToLong();
            var sql = $@"SELECT LOWER(TRIM(TRAILING '0' FROM HEX(a.Id))) AS Id
, Title
, Content
, b.Name Category
, ViewsCount ViewCount
, a.CreateTime
, a.ModifyTime
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.Category_Id
WHERE a.Id = {id};
";
            var article = await conn.QueryFirstOrDefaultAsync<ArticleDetailDto>(sql);
            if (article == null) throw new MessageException("文章未找到");
            article.Tags = await conn.QueryAsync<string>($"SELECT a.Name FROM blog_tags a JOIN blog_articletags b ON b.Tag_Id = a.Id WHERE b.Article_Id = {id};");
            var allComments = await conn.QueryAsync<CommentDto>($@"SELECT LOWER(TRIM(TRAILING '0' FROM HEX(c.Id))) AS Id
, LOWER(TRIM(TRAILING '0' FROM HEX(c.Parent_Id))) AS ParentId
, LOWER(TRIM(TRAILING '0' FROM HEX(c.Article_Id))) AS ArticleId
, u.Name UserName
, u.Email
, r.Name Role
, c.Content
, c.ReplyTo
, c.CommentTime
, c.ModifyTime
FROM blog_comments c
JOIN base_user u ON u.Id = c.User_Id AND c.IsDeleted=0 AND u.IsDeleted=0
JOIN base_userrole ur ON u.Id=ur.User_Id
JOIN base_role r ON ur.Role_Id=r.Id AND r.IsDeleted=0
WHERE c.Article_Id = {id}");
            article.Comments = allComments.Where(v => v.ParentId == null).ToList();
            foreach (var comment in article.Comments)
            {
                comment.Children = allComments.Where(v => v.ParentId == comment.Id).ToList();
            }
            return article;
        }

        [DapperAot]
        public async Task AddViewsCount(string hexId)
        {
            var curr = currProvider.GetCurrUser();
            if (curr != null && curr.IsAdmin) return;
            var id = hexId.HexToLong();
            await conn.ExecuteAsync($"update blog_articles set ViewsCount = ViewsCount + 1 where Id = {id};");
        }

        [DapperAot]
        public async Task<ArticleDetailDto> CreateArticle(ArticleCreateDto createDto)
        {
            //logger.LogError("CreateDto: \ncategory - {0}\ncontent - {1}\nstate - {2}\n tags - {3}\ntitle - {4}",
            //    createDto.Category,
            //    createDto.Content,
            //    "",
            //    string.Join(",", createDto.Tags),
            //    createDto.Title);
            var article_Id = idGen.CreateId();
            var sql = new StringBuilder();
            var param = new DynamicParameters();

            var allTags = await conn.QueryAsync<TagModel>("SELECT Id,TKey,Name FROM Blog_Tags WHERE IsDeleted=0;");
            var newTags = createDto.Tags.Where(v => !allTags.Any(t => t.TKey == v.ToLower())).ToList();
            var i = 0;
            for (; i < newTags.Count; i++)
            {
                var tag = newTags[i];
                sql.Append(@$"INSERT INTO Blog_Tags(Id,TKey,Name) VALUES(@Id_{i},@Tkey_{i},@Name_{i});
INSERT INTO Blog_ArticleTags(Article_Id,Tag_Id) VALUES(@Article_Id,@Id_{i});");
                param.Add($"@Id_{i}", idGen.CreateId());
                param.Add($"@TKey_{i}", tag.ToLower());
                param.Add($"@Name_{i}", tag);
            }
            var oldTags = allTags.Where(v => createDto.Tags.Any(t => t.ToLower() == v.TKey)).ToList();
            for (var j = 0; j < oldTags.Count; j++)
            {
                var tag = oldTags[j];
                sql.Append($@"INSERT INTO Blog_ArticleTags(Article_Id,Tag_Id) VALUES(@Article_Id,@Id_{i});");
                param.Add($"@Id_{i}", tag.Id);
                i++;
            }
            var existCategoryId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT Id FROM Blog_Categories WHERE Name=@Category;", new { createDto.Category });
            if (existCategoryId == null)
            {
                existCategoryId = idGen.CreateId();
                sql.Append($"INSERT INTO Blog_Categories(Id,IsDeleted,Name,CreateTime) VALUES(@Id_{i},0,@Name_{i},NOW());\r\n");
                param.Add($"@Id_{i}", existCategoryId);
                param.Add($"@Name_{i}", createDto.Category);
                i++;
            }
            sql.Append($"INSERT INTO Blog_Articles(Id,Title,Content,Category_Id,ViewsCount,CreateTime,ModifyTime) VALUES(@Article_Id,@Title,@Content,@Category_Id,0,now(),now());");
            param.Add($"@Article_Id", article_Id);
            param.Add("@Title", createDto.Title);
            param.Add("@Content", createDto.Content);
            param.Add("@Category_Id", existCategoryId);
            //var p = param.ParameterNames.Select(v => $"{v}: {param.Get<dynamic>(v).ToString()}").Join(';');
            var q = sql.ToString();
            //logger.LogDebug("Sql: {0}\nParam: {1}", q, p);
            await conn.ExecuteAsync(q, param);
            return await GetArticle(article_Id.LongToHex());
        }

        [DapperAot]
        public async Task UpdateArticle(string hexId, ArticleUpdateDto article)
        {
            var id = hexId.HexToLong();
            var oldTKeys = (await conn.QueryAsync<string>(@$"SELECT b.TKey
FROM Blog_ArticleTags a
JOIN Blog_Tags b ON a.Tag_Id=b.Id AND b.IsDeleted=0
WHERE a.Article_Id={id}")).AsList();
            var newTags = article.Tags.Select(v => new { TKey = v.ToLower(), Name = v });
            var newTKeys = article.Tags.Select(v => v.ToLower()).ToList();

            var needAdd = newTags.Where(v => !oldTKeys.Contains(v.TKey)).ToList();
            var needDel = oldTKeys.Where(v => !newTKeys.Contains(v)).ToList();
            var sql = new StringBuilder();
            var param = new DynamicParameters();
            param.Add($"@id", id);
            for (var i = 0; i < needAdd.Count; i++)
            {
                var tag = needAdd[i];
                var tid = await conn.ExecuteScalarAsync<long?>("SELECT Id FROM Blog_Tags WHERE TKey=@TKey;", new { tag.TKey });
                if (tid == null)
                {
                    tid = idGen.CreateId();
                    sql.AppendLine($"INSERT INTO Blog_Tags(Id,TKey,Name) VALUES(@tid_{i},@tkey_{i},@name_{i});");
                    param.Add($"@tkey_{i}", tag.TKey);
                    param.Add($"@name_{i}", tag.Name);
                }
                sql.AppendLine($"INSERT INTO Blog_ArticleTags(Article_Id,Tag_Id) VALUES(@id,@tid_{i});");
                param.Add($"@tid_{i}", tid);
            }
            if (needDel.Any())
            {
                sql.AppendLine($"DELETE FROM Blog_ArticleTags WHERE Article_Id=@id AND Tag_Id in (SELECT Id FROM Blog_Tags WHERE TKey in @needdel);");
                param.Add("@needdel", needDel);
            }
            var categoryId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT Id FROM Blog_Categories WHERE Name=@name", new { name = article.Category });
            if (categoryId == null)
            {
                categoryId = idGen.CreateId();
                sql.AppendLine("INSERT INTO Blog_Categories(Id,Name,CreateTime) VALUES(@cid,@cname,now());");
                param.Add("@cname", article.Category);
            }
            sql.AppendLine("UPDATE Blog_Articles SET Title=@title, Content=@content, ModifyTime=now(), Category_Id=@cid WHERE Id=@id;");
            param.Add("@title", article.Title);
            param.Add("@content", article.Content);
            param.Add("@cid", categoryId);
            await conn.ExecuteAsync(sql.ToString(), param);
        }

        [DapperAot]
        public async Task<bool> DeleteArticle(string hexId)
        {
            var id = hexId.HexToLong();
            await conn.ExecuteAsync($"UPDATE Blog_Articles SET IsDeleted=1 WHERE Id={id};");
            return true;
        }
    }
}
