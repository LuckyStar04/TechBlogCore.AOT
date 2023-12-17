using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TechBlogCore.AOT.DtoParams;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Entities;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.RestApi.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class ArticleService
    {
        private readonly MySqlConnection conn;

        public ArticleService(MySqlConnection conn)
        {
            this.conn = conn;
        }

        [DapperAot]
        public async Task<PagedList<ArticleListDto>> GetArticles(ArticleDtoParam param)
        {
            using (conn)
            {
                conn.Open();
                var cond = "";
                if (param.IncludeDeleted == false)
                {
                    cond += " AND a.State <> 0";
                }
                if (!param.Category.IsNullOrWhiteSpace())
                {
                    cond += " AND b.Name = @Category";
                }
                if (!param.Tag.IsNullOrWhiteSpace())
                {
                    cond += " AND a.Id in (SELECT ArticleId FROM blog_articletags a JOIN blog_tags b ON a.TagId=b.Id WHERE b.Name = @Tag)";
                }
                if (!param.Keyword.IsNullOrWhiteSpace())
                {
                    cond += " AND a.Title LIKE @Keyword";
                }

                var query_sql = $@"SELECT a.Id
, Title
, CONCAT(
    SUBSTRING(Content, 1, 260)
    , CASE
        WHEN LENGTH(Content) > 260 THEN '…'
        ELSE ''
    END) Content
, b.Name Category
, ViewsCount ViewCount
,(SELECT count(*) FROM blog_comments c WHERE c.ArticleId=a.Id) CommentCount
, a.CreateTime
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.CategoryId
WHERE 1=1 {cond}
ORDER BY a.CreateTime desc LIMIT {(param.PageNumber-1)*param.PageSize}, {param.PageSize}";
                var count_sql = $@"SELECT count(*)
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.CategoryId
WHERE 1=1 {cond}";

                return new PagedList<ArticleListDto>
                (
                    (await conn.QueryAsync<ArticleListDto>(query_sql, param)).AsList(),
                    await conn.ExecuteScalarAsync<int>(count_sql, param),
                    param.PageNumber,
                    param.PageSize
                );
            }
        }

        [DapperAot]
        public async Task<ArticleDetailDto> GetArticle(int id)
        {
            using (conn)
            {
                conn.Open();
                var sql = $@"SELECT a.Id
, Title
, Content
, b.Name Category
, CASE a.State WHEN 0 THEN 'Deleted' WHEN 1 THEN 'Active' WHEN 2 THEN 'Modified' ELSE '' END AS State
, ViewsCount ViewCount
, a.CreateTime
, a.ModifyTime
FROM Blog_Articles a
JOIN blog_categories b ON b.Id = a.CategoryId
WHERE a.Id = {id};
";
                var article = await conn.QueryFirstOrDefaultAsync<ArticleDetailDto>(sql);
                if (article == null) throw new MessageException("文章未找到");
                article.Tags = await conn.QueryAsync<string>($"SELECT a.Name FROM blog_tags a JOIN blog_articletags b ON b.TagId = a.Id WHERE b.ArticleId={id}");
                var allComments = await conn.QueryAsync<CommentDto>($@"SELECT c.Id
, c.ParentId
, c.ArticleId
, u.UserName
, u.Email
, u.Role
, c.Content
, c.ReplyTo
, c.CommentTime
, c.ModifyTime
FROM blog_comments c
JOIN aspnetusers u ON u.Id = c.Blog_UserId 
WHERE c.ArticleId = {id}");
                article.Comments = allComments.Where(v => v.ParentId == null).ToList();
                foreach (var comment in article.Comments)
                {
                    comment.Children = allComments.Where(v => v.ParentId == v.Id).ToList();
                }
                return article;
            }
        }
    }
}
