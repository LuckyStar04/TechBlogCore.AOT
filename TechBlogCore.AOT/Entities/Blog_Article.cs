namespace TechBlogCore.AOT.Entities
{
    public class Blog_Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public Blog_Category Category { get; set; }
        public State State { get; set; }
        public int ViewsCount { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ModifyTime { get; set; }
        public IEnumerable<Blog_Tag> Tags { get; set; }
        public IEnumerable<Blog_Comment> Comments { get; set; }
    }

    public enum State
    {
        Deleted = 0,
        Active = 1,
        Modified = 2
    }
}
