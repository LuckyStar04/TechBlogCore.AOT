using TechBlogCore.AOT.Dtos;

namespace TechBlogCore.AOT.Providers
{
    public interface ICurrUserProvider
    {
        public UserDto GetCurrUser();
    }
}
