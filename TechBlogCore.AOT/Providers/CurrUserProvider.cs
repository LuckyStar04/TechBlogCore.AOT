using System.Security.Claims;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Services;

namespace TechBlogCore.AOT.Providers
{
    public interface ICurrUserProvider
    {
        public UserDto? GetCurrUser();
    }

    public class CurrUserProvider : ICurrUserProvider
    {
        private readonly ClaimsPrincipal? user;

        public CurrUserProvider(IHttpContextAccessor httpContextAccessor)
        {
            user = httpContextAccessor.HttpContext?.User;
        }
        public UserDto? GetCurrUser()
        {
            if (user == null) return null;
            var id = user.FindFirstValue(BlogClaimTypes.Id);
            if (id == null) return null;
            return new UserDto
            {
                Id = id,
                Account = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = user.FindFirstValue(BlogClaimTypes.Email),
                Name = user.FindFirstValue(BlogClaimTypes.Name),
                Roles = user.Claims.Where(v => v.Type == BlogClaimTypes.Role).Select(v => v.Value).ToArray(),
                IsAdmin = user.Claims.Any(v => v.Type == BlogClaimTypes.Role && v.Value == "Admin"),
            };
        }
    }
}
