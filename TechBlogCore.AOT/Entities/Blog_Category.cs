namespace TechBlogCore.AOT.Entities
{
    public class Blog_Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
        public IEnumerable<Blog_Article> Articles { get; set; }
    }
}
