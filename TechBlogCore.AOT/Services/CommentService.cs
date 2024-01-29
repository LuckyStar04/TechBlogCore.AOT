using Dapper;
using IdGen;
using MySqlConnector;
using System.Security.Cryptography;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.RestApi.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class CommentService
    {
        private readonly MySqlConnection conn;
        private readonly IdGenerator idGen;

        public CommentService(MySqlConnection conn, IdGenerator idGen)
        {
            this.conn = conn;
            this.idGen = idGen;
        }

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

            var comments = allComments.Where(v => v.Parent_Id == null).ToList();
            foreach (var comment in comments)
            {
                comment.Children = allComments.Where(v => v.Parent_Id == comment.Id).ToList();
            }
            return new PagedList<CommentDto>
            (
                comments.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), comments.Count, pageNumber, pageSize
            );
        }

        public async Task<CommentDto> GetComment(string articleId, string commentId)
        {
            var id = articleId.HexToLong();
            var cid = commentId.HexToLong();
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
WHERE Article_Id={id} AND (a.Id={cid} OR a.Parent_Id={cid}) AND a.IsDeleted=0;
")).AsList();
            var comment = allComments.First(v => v.Parent_Id == null);
            comment.Children = allComments.Where(v => v.Parent_Id == comment.Id).ToList();
            return comment;
        }
    }
}
