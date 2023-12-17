using Dapper;
using MySqlConnector;
using TechBlogCore.AOT.Dtos;

namespace TechBlogCore.AOT.Services
{
    public class CategoryService
    {
        private readonly MySqlConnection conn;

        public CategoryService(MySqlConnection conn)
        {
            this.conn = conn;
        }

        [DapperAot]
        public async Task<IEnumerable<CategoryDto>> GetCategories(int size)
        {
            using (conn)
            {
                conn.Open();
                return await conn.QueryAsync<CategoryDto>($@"SELECT *
FROM
(SELECT t.Id
, t.Name
, (SELECT count(*)
    FROM blog_articles a
    WHERE a.CategoryId = t.Id) Count
FROM blog_categories t
) _t WHERE Count > 0 ORDER BY _t.Count DESC LIMIT {size}");
            }
        }
    }
}
