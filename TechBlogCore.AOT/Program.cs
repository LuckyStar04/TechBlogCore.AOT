using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Text.Json.Serialization;
using TechBlogCore.AOT.DtoParams;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Entities;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Services;
using TechBlogCore.RestApi.Helpers;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddTransient<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ArticleService, ArticleService>();
builder.Services.AddScoped<TagService, TagService>();
builder.Services.AddScoped<CategoryService, CategoryService>();
builder.Logging.AddConsole();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.ConfigureExceptionHandler(logger);

var articles = app.MapGroup("/api/articles");

articles.MapGet("/",
    async (
        string? Category,
        string? Tag,
        string? Keyword,
        ArticleService articleService,
        HttpContext context,
        int PageSize = 30,
        int PageNumber = 1
    ) =>
    {
        var articles = await articleService.GetArticles(new ArticleDtoParam { PageSize = PageSize, PageNumber = PageNumber, Category = Category, Tag = Tag, Keyword = Keyword, IncludeDeleted = false });

        var paginationMetadata = new ArticleMetadata
        {
            totalCount = articles.TotalCount,
            pageSize = articles.PageSize,
            currentPage = articles.CurrentPage,
            totalPages = articles.TotalPages
        };
        context.Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(paginationMetadata, typeof(ArticleMetadata), AppJsonSerializerContext.Default));
        return articles;
    });

articles.MapGet("{id:int:min(1)}", (int id, ArticleService articleService) => articleService.GetArticle(id));

app.MapGet("/api/tags", (TagService service, int size = 50) => service.GetTags(size));
app.MapGet("/api/categories", (CategoryService service, int size = 50) => service.GetCategories(size));

app.Run();


[JsonSerializable(typeof(PagedList<ArticleListDto>))]
[JsonSerializable(typeof(ArticleListDto))]
[JsonSerializable(typeof(ArticleDetailDto))]
[JsonSerializable(typeof(ArticleDtoParam))]
[JsonSerializable(typeof(ArticleMetadata))]
[JsonSerializable(typeof(IEnumerable<TagDto>))]
[JsonSerializable(typeof(IEnumerable<CategoryDto>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
