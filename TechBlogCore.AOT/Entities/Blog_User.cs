namespace TechBlogCore.AOT.Entities
{
    public class Blog_User
    {
        public string Role { get; set; }
        public IEnumerable<Blog_Comment> Comments { get; set; }
        //public IEnumerable<Chat_Message> Messages { get; set; }
    }
}
