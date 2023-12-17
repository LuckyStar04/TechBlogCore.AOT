using Dapper;
using MySqlConnector;
using TechBlogCore.AOT.Dtos;

namespace TechBlogCore.AOT.Services
{
    public class TagService
    {
        private readonly MySqlConnection conn;

        public TagService(MySqlConnection conn)
        {
            this.conn = conn;
        }

        [DapperAot]
        public async Task<IEnumerable<TagDto>> GetTags(int size)
        {
            using (conn)
            {
                conn.Open();
                return await conn.QueryAsync<TagDto>($@"SELECT *
FROM
(SELECT t.Id
, t.Name
, (SELECT count(*)
    FROM blog_articles a
    JOIN blog_articletags at ON at.ArticleId = a.Id AND a.State<>0
    WHERE at.TagId = t.Id) Count
FROM blog_tags t
) _t WHERE Count > 0 ORDER BY _t.Count DESC LIMIT {size}");
            }
        }
    }
}
