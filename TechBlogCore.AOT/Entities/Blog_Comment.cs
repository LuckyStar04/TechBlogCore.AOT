namespace TechBlogCore.AOT.Entities
{
    public class Blog_Comment
    {
        public int Id { get; set; }
        public string Blog_UserId { get; set; }
        public Blog_User User { get; set; }
        public string ReplyTo { get; set; }
        public int ArticleId { get; set; }
        public Blog_Article Article { get; set; }
        public int? ParentId { get; set; }
        public Blog_Comment Parent { get; set; }
        public IEnumerable<Blog_Comment> Children { get; set; }
        public string Content { get; set; }
        public DateTime CommentTime { get; set; }
        public DateTime? ModifyTime { get; set; }
        public State State { get; set; }
    }
}
