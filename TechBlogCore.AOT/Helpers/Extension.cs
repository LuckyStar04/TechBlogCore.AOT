namespace TechBlogCore.AOT.Helpers
{
    public static class Extension
    {
        public static bool IsNullOrWhiteSpace(this string? s)
        {
            return string.IsNullOrWhiteSpace(s);
        }
    }
}
