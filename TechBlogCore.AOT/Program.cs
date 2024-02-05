using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using TechBlogCore.AOT.DtoParams;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Providers;
using TechBlogCore.AOT.Services;
using TechBlogCore.RestApi.Helpers;

var builder = WebApplication.CreateSlimBuilder(args);
var configuration = builder.Configuration;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    //options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine();
});

builder.Services.AddTransient(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ArticleService, ArticleService>();
builder.Services.AddScoped<TagService, TagService>();
builder.Services.AddScoped<CategoryService, CategoryService>();
builder.Services.AddScoped<AuthService, AuthService>();
builder.Services.AddScoped<CommentService, CommentService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<ICurrUserProvider, CurrUserProvider>();
builder.Services.AddSingleton<IdGen.IdGenerator, IdGen.IdGenerator>((_) => new IdGen.IdGenerator(1000));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = configuration["JWT:ValidAudience"],
        ValidIssuer = configuration["JWT:ValidIssuer"],
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", b => b.RequireClaim(BlogClaimTypes.Role, "Admin"));
    o.AddPolicy("NeedLogin", b => b.RequireClaim(BlogClaimTypes.Role, "Admin", "CommonUser"));
});
builder.Logging.AddConsole();
builder.Services.AddMemoryCache();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.ConfigureExceptionHandler(logger);

#region 文章详情
var articles = app.MapGroup("/api/articles");

articles.MapGet("/", async (string? Category, string? Tag, string? Keyword, ArticleService articleService, HttpContext context, int PageSize = 30, int PageNumber = 1) =>
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

articles.MapGet("{id}", (string id, ArticleService articleService)
    => articleService.GetArticle(id)).WithName("GetArticleById");
articles.MapPost("/", async (ArticleCreateDto createDto, ArticleService articleService) =>
{
    var entity = await articleService.CreateArticle(createDto);
    return Results.CreatedAtRoute("GetArticleById", RouteValueDictionary.FromArray([new("id", entity.Id)]), entity);
}).RequireAuthorization("AdminOnly");
articles.MapPut("{id}", (string id, ArticleUpdateDto updateDto, ArticleService articleService)
    => articleService.UpdateArticle(id, updateDto)).RequireAuthorization("AdminOnly");
articles.MapDelete("{id}", (string id, ArticleService articleService)
    => articleService.DeleteArticle(id)).RequireAuthorization("AdminOnly");
#endregion

#region 文章标签/分类
app.MapGet("/api/tags", (TagService service, int size = 50)
    => service.GetTags(size));
app.MapGet("/api/categories", (CategoryService service, int size = 50)
    => service.GetCategories(size));
#endregion

#region 鉴权
var auth = app.MapGroup("/api/auth");
auth.MapPost("login", (AuthService service, LoginDto dto)
    => service.Login(dto));
auth.MapGet("status", (AuthService service)
    => service.GetStatus());
auth.MapPost("register", (AuthService service, RegisterDto dto)
    => service.Register(dto));
#endregion

#region 评论
var comment = app.MapGroup("api/articles/{articleId}/comments");
comment.MapGet("/", (string articleId, string? parentId, int PageSize, int PageNumber, CommentService service)
    => service.GetComments(articleId, parentId, PageNumber, PageSize)).WithName("GetCommentById");
comment.MapGet("{commentId}", (string articleId, string commentId, CommentService service)
    => service.GetComment(articleId, commentId));
comment.MapPost("/", (string articleId, CommentCreateDto dto, CommentService service)
    => service.CreateComment(articleId, dto)).RequireAuthorization("NeedLogin");
comment.MapPut("{commentId}", (string articleId, string commentId, CommentModifyDto dto, CommentService service)
    => service.ModifyComment(articleId, commentId, dto)).RequireAuthorization("NeedLogin");
#endregion

app.Run();


[JsonSerializable(typeof(PagedList<ArticleListDto>))]
[JsonSerializable(typeof(ArticleListDto))]
[JsonSerializable(typeof(ArticleDetailDto))]
[JsonSerializable(typeof(ArticleDtoParam))]
[JsonSerializable(typeof(ArticleMetadata))]
[JsonSerializable(typeof(IEnumerable<TagDto>))]
[JsonSerializable(typeof(IEnumerable<CategoryDto>))]
[JsonSerializable(typeof(ArticleCreateDto))]
[JsonSerializable(typeof(ArticleUpdateDto))]
[JsonSerializable(typeof(LoginDto))]
[JsonSerializable(typeof(RegisterDto))]
[JsonSerializable(typeof(UserStatusDto))]
[JsonSerializable(typeof(UserPasswordVerifyDto))]
[JsonSerializable(typeof(UserRoleDto))]
[JsonSerializable(typeof(ResponseDto<string>))]
[JsonSerializable(typeof(CommentModifyDto))]
[JsonSerializable(typeof(CommentCreateDto))]
[JsonSerializable(typeof(CommentCreateModel))]
[JsonSerializable(typeof(PagedList<CommentDto>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
