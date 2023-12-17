namespace TechBlogCore.AOT.Entities
{
    public class Blog_Tag
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public IEnumerable<Blog_Article> Articles { get; set; }
    }
}
