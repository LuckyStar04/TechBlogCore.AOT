using Dapper;
using IdGen;
using MySqlConnector;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Providers;
using TechBlogCore.RestApi.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class CommentService
    {
        private readonly MySqlConnection conn;
        private readonly IdGenerator idGen;
        private readonly ICurrUserProvider currProvider;

        public CommentService(MySqlConnection conn, IdGenerator idGen, ICurrUserProvider currProvider)
        {
            this.conn = conn;
            this.idGen = idGen;
            this.currProvider = currProvider;
        }

        [DapperAot]
        public async Task<PagedList<CommentDto>> GetComments(string articleId, string? parentId, int pageNumber, int pageSize)
        {
            var id = articleId.HexToLong();
            var pid = parentId?.HexToLong();
            var exists = await conn.ExecuteScalarAsync<int?>($"SELECT 1 FROM Blog_Articles WHERE Id={id}");
            if (exists == null)
            {
                throw new MessageException("文章未找到");
            }
            var allComments = (await conn.QueryAsync<CommentDto>($@"SELECT
  LOWER(HEX(a.Id)) AS Id
  , LOWER(HEX(a.Parent_Id)) AS Parent_Id
  , LOWER(TRIM(TRAILING '0' FROM HEX(a.Id))) AS Article_Id
  , b.Name AS UserName
  , b.Email AS Email
  , a.Content
  , a.ReplyTo
  , a.CommentTime
  , a.ModifyTime
FROM Blog_Comments a
JOIN Base_User b ON a.User_Id=b.Id
WHERE Article_Id={id} AND a.IsDeleted=0 {(pid == null ? "" : "AND a.Parent_Id=" + pid)};
")).AsList();

            var comments = allComments.Where(v => v.ParentId == null).ToList();
            foreach (var comment in comments)
            {
                comment.Children = allComments.Where(v => v.ParentId == comment.Id).ToList();
            }
            return new PagedList<CommentDto>
            (
                comments.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), comments.Count, pageNumber, pageSize
            );
        }

        [DapperAot]
        public async Task<CommentDto?> GetComment(string articleId, string commentId)
        {
            var id = articleId.HexToLong();
            var cid = commentId.HexToLong();
            var exists = await conn.ExecuteScalarAsync<int?>($"SELECT 1 FROM Blog_Articles WHERE Id={id}");
            if (exists == null)
            {
                throw new MessageException("文章未找到");
            }
            var allComments = (await conn.QueryAsync<CommentDto>($@"SELECT
  LOWER(TRIM(TRAILING '0' FROM HEX(a.Id))) AS Id
  , LOWER(TRIM(TRAILING '0' FROM HEX(a.Parent_Id))) AS ParentId
  , LOWER(TRIM(TRAILING '0' FROM HEX(a.Article_Id))) AS ArticleId
  , b.Name AS UserName
  , b.Email AS Email
  , LOWER(TRIM(TRAILING '0' FROM HEX(a.User_Id))) AS UserId
  , a.Content
  , a.ReplyTo
  , a.CommentTime
  , a.ModifyTime
FROM Blog_Comments a
JOIN Base_User b ON a.User_Id=b.Id
WHERE Article_Id={id} AND (a.Id={cid} OR a.Parent_Id={cid}) AND a.IsDeleted=0;
")).AsList();
            var comment = allComments.FirstOrDefault(v => v.Id == commentId);
            if (comment != null) comment.Children = allComments.Where(v => v.ParentId == comment.Id).ToList();
            return comment;
        }

        [DapperAot]
        public async Task<IResult> CreateComment(string articleId, CommentCreateDto dto)
        {
            var user = currProvider.GetCurrUser();
            if (user == null)
            {
                return Results.Unauthorized();
            }
            var sql = @"INSERT INTO Blog_Comments(Id,User_Id,ReplyTo,Article_Id,Parent_Id,Content,CommentTime,ModifyTime)
VALUES(@Id,@User_Id,@ReplyTo,@Article_Id,@Parent_Id,@Content,NOW(),NOW());";
            var model = new CommentCreateModel
            {
                Id = idGen.CreateId(),
                User_Id = user.Id.HexToLong(),
                ReplyTo = dto.ReplyTo,
                Article_Id = articleId.HexToLong(),
                Parent_Id = dto.ParentId == null ? null : dto.ParentId.HexToLong(),
                Content = dto.Content,
            };
            await conn.ExecuteAsync(sql, model);
            var entity = await GetComment(articleId, model.Id.LongToHex());
            return Results.CreatedAtRoute("GetCommentById", RouteValueDictionary.FromArray([new("id", entity.Id)]), entity);
        }

        [DapperAot]
        public async Task<IResult> ModifyComment(string articleId, string commentId, CommentModifyDto dto)
        {
            var comment = await GetComment(articleId, commentId);
            if (comment == null)
            {
                throw new MessageException("文章或者评论未找到！");
            }
            var user = currProvider.GetCurrUser();
            if (user == null)
            {
                return Results.Unauthorized();
            }
            if (!user.Roles.Contains("Admin") && comment.UserId != user.Id)
            {
                throw new MessageException("不能修改他人评论");
            }
            var cid = commentId.HexToLong();
            var sql = $"UPDATE Blog_Comments SET Content=@Content WHERE Id={cid}";
            var effects = await conn.ExecuteAsync(sql, dto) > 0;
            if (effects)
                return Results.Ok();
            else
                return Results.BadRequest();
        }
    }
}
