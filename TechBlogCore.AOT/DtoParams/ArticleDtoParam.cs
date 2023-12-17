namespace TechBlogCore.AOT.DtoParams
{
    public class ArticleDtoParam
    {
        public int PageSize { get; set; } = 30;
        public int PageNumber { get; set; } = 1;
        public string? Category { get; set; } = null;
        public string? Tag { get; set; } = null;
        public string? Keyword { get; set; } = null;
        public bool IncludeDeleted { get; set; } = false;
    }

    public class ArticleMetadata
    {
        public int totalCount { get; set; }
        public int pageSize { get; set; }
        public int currentPage { get; set; }
        public int totalPages { get; set; }
    }
}
