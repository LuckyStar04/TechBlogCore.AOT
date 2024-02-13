namespace TechBlogCore.AOT.Dtos
{
    public class TagDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class TagModel
    {
        public long Id { get; set; }
        public string TKey { get; set; }
        public string Name { get; set; }
    }
}
