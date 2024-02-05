using Dapper;
using IdGen;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;
using TechBlogCore.AOT.Providers;

namespace TechBlogCore.AOT.Services
{
    public class AuthService
    {
        private readonly MySqlConnection conn;
        private readonly IConfiguration configuration;
        private readonly IMemoryCache cache;
        private readonly IdGenerator idGen;
        private readonly ICurrUserProvider currProvider;

        public AuthService(
            MySqlConnection conn,
            IConfiguration configuration,
            IMemoryCache cache,
            IdGen.IdGenerator idGen,
            ICurrUserProvider currProvider)
        {
            this.conn = conn;
            this.configuration = configuration;
            this.cache = cache;
            this.idGen = idGen;
            this.currProvider = currProvider;
        }

        const int keySize = 32, iterations = 350000;

        [DapperAot]
        public async Task<IResult> Login(LoginDto dto)
        {
            var user = await conn.QueryFirstOrDefaultAsync<UserPasswordVerifyDto>($"select Id,Account,Name,Email,Avatar_Id,IsDisabled,IsDeleted,IsAdmin,Hash,Salt from Base_User where Account=@Username;", dto);
            if (user == null)
            {
                SetFailedCount(dto.Username);
                return TypedResults.Unauthorized();
            }
            //验证密码
            if (!VerifyPassword(dto.Password, user.Hash, user.Salt))
            {
                SetFailedCount(dto.Username);
                return TypedResults.Unauthorized();
            }
            var roles = (await conn.QueryAsync<string>(@$"SELECT a.Encode
FROM Base_Role a
JOIN Base_UserRole b ON a.Id=b.Role_Id
WHERE b.User_Id={user.Id} AND a.IsDeleted=0;")).AsList();

            return TypedResults.Ok(new ResponseDto<string>
            {
                Data = GetToken(user, roles)
            });
        }

        [DapperAot]
        public async Task<IResult> Register(RegisterDto dto)
        {
            var param = new DynamicParameters();
            param.Add("@account", dto.Username);
            param.Add("@email", dto.Email);
            param.Add("@password", dto.Password);
            var userExists = await conn.ExecuteScalarAsync<int>(@"SELECT 1 FROM Base_User WHERE Account=@account OR Email=@email;", param);
            if (userExists == 1)
            {
                return TypedResults.BadRequest(new ResponseDto<string>
                {
                    Code = 1,
                    Msg = "用户或者邮箱已存在，请尝试更换"
                });
            }
            var hash = HashPasword(dto.Password, out var salt);
            var user = new UserPasswordVerifyDto
            {
                Id = idGen.CreateId(),
                Account = dto.Username,
                Name = dto.Username,
                Email = dto.Email,
                Hash = hash,
                Salt = salt,
                IsDisabled = false,
                IsAdmin = false,
                IsDeleted = false,
                Avatar_Id = 0,
            };
            var role_id = await conn.ExecuteScalarAsync<long?>(@"SELECT Id FROM Base_Role WHERE IsDefault=1;");
            if (role_id > 0)
            {
                var userRole = new UserRoleDto
                {
                    User_Id = user.Id,
                    Role_Id = role_id.Value,
                };
                await conn.ExecuteAsync("INSERT INTO Base_UserRole(User_Id,Role_Id) VALUES(@User_Id,@Role_Id);", userRole);
            }
            await conn.ExecuteAsync("INSERT INTO Base_User(Id,Account,Name,Email,Hash,Salt,IsDisabled,IsAdmin,IsDeleted,Avatar_Id) VALUES(@Id,@Account,@Name,@Email,@Hash,@Salt,@IsDisabled,@IsAdmin,@IsDeleted,@Avatar_Id);", user);
            return TypedResults.Ok(new ResponseDto<string>
            {
                Msg = "注册成功。",
            });
        }

        private void SetFailedCount(string username)
        {
            const int MaxFailedCount = 5;
            const int ExpireMinutes = 15;
            if (cache.TryGetValue($"LOGIN_FAILED_{username}", out int t))
            {
                if (t >= MaxFailedCount)
                {
                    throw new MessageException($"用户名或密码错误已达 {MaxFailedCount} 次，已经锁定。请 {ExpireMinutes} 分钟后再试");
                }
                else
                {
                    cache.Set($"LOGIN_FAILED_{username}", t + 1, TimeSpan.FromMinutes(ExpireMinutes));
                    throw new MessageException($"用户名或密码错误");
                }
            }
            else
            {
                cache.Set($"LOGIN_FAILED_{username}", 1, TimeSpan.FromMinutes(ExpireMinutes));
                throw new MessageException($"用户名或密码错误");
            }
        }

        public IResult GetStatus()
        {
            var curr = currProvider.GetCurrUser();
            return TypedResults.Ok(new UserStatusDto { role = curr?.Roles.FirstOrDefault(), user = curr?.Account, name = curr?.Name, email = curr?.Email });
        }

        private bool VerifyPassword(string password, string hash, string saltStr)
        {
            var salt = Convert.FromHexString(saltStr);
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, keySize);
            return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromHexString(hash));
        }

        private string HashPasword(string password, out string saltStr)
        {
            var salt = RandomNumberGenerator.GetBytes(keySize);
            saltStr = BitConverter.ToString(salt).Replace("-", "");

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA512,
                keySize);

            return Convert.ToHexString(hash);
        }

        private string GetToken(UserPasswordVerifyDto user, IEnumerable<string> roles)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
            var claims = new List<Claim>
            {
                new Claim(BlogClaimTypes.Id, user.Id.LongToHex()),
                new Claim(BlogClaimTypes.Sub, user.Account),
                new Claim(BlogClaimTypes.Name, user.Name),
                new Claim(BlogClaimTypes.Email, user.Email),
            };
            if (user.IsAdmin)
                claims.Add(new Claim(BlogClaimTypes.Role, "Admin"));
            else
                claims.AddRange(roles.Select(v => new Claim(BlogClaimTypes.Role, v)));

            var token = new JwtSecurityToken(
                issuer: configuration["JWT:ValidIssuer"],
                audience: configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddDays(3),
                notBefore: DateTime.Now,
                claims: claims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public static class BlogClaimTypes
    {
        public const string Role = "TechBlogCore.Role";
        public const string Email = "TechBlogCore.Email";
        public const string Name = "TechBlogCore.Name";
        public const string Sub = "sub";
        public const string Id = "TechBlogCore.Id";
    }
}
