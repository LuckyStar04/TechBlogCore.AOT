using Dapper;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TechBlogCore.AOT.Dtos;
using TechBlogCore.AOT.Helpers;

namespace TechBlogCore.AOT.Services
{
    public class AuthService
    {
        private readonly MySqlConnection conn;
        private readonly IConfiguration configuration;

        public AuthService(
            MySqlConnection conn,
            IConfiguration configuration)
        {
            this.conn = conn;
            this.configuration = configuration;
        }

        const int keySize = 32, iterations = 350000;

        [DapperAot]
        public async Task<IResult> Login(LoginDto dto)
        {
            const int maxAttempts = 5, lockMinutes = 15;
            var user = await conn.QueryFirstOrDefaultAsync<UserPasswordVerifyDto>($"select Id,Account,Name,Email,Avatar_Id,IsDisabled,IsDeleted,IsAdmin,IsLock,LockRetry,LockExpire,Hash,Salt from Base_User where Account=@Username;", dto);
            if (user == null)
            {
                return TypedResults.Unauthorized();
            }
            if (user.IsLock)
            {
                if (user.LockExpire > DateTime.UtcNow)
                {
                    throw new MessageException("密码错误次数过多，请稍后尝试");
                }
                else
                {
                    await conn.ExecuteAsync($"UPDATE Base_User SET IsLock=0,LockRetry=0,LockExpire=NULL WHERE Id={user.Id};");
                }
            }
            //验证密码
            if (!VerifyPassword(dto.Password, user.Hash, user.Salt))
            {
                user.LockRetry++;
                if (user.LockRetry > maxAttempts)
                {
                    await conn.ExecuteAsync($"UPDATE Base_User SET IsLock=1,LockRetry=0,LockExpire='{DateTime.UtcNow.AddMinutes(lockMinutes):yyyy-MM-dd HH:mm:ss}' WHERE Id={user.Id};");
                    throw new MessageException("密码错误次数过多，请稍后尝试");
                }
                else
                {
                    await conn.ExecuteAsync($"UPDATE Base_User SET LockRetry={user.LockRetry} WHERE Id={user.Id};");
                    throw new MessageException("用户名或者密码错误");
                }
            }
            await conn.ExecuteAsync($"UPDATE Base_User SET IsLock=0,LockRetry=0,LockExpire=NULL WHERE Id={user.Id};");
            var roles = (await conn.QueryAsync<string>(@$"SELECT a.Name
FROM Base_Role a
JOIN Base_UserRole b ON a.Id=b.Role_Id
WHERE b.User_Id={user.Id} AND a.IsDeleted=0;")).AsList();

            return TypedResults.Ok(new ResponseDto<string>
            {
                Data = GetToken(user.Account, user.Email, user.IsAdmin, roles)
            });
        }

        public IResult GetStatus(ClaimsPrincipal User)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            return TypedResults.Ok(new UserStatusDto { role = role, user = user, email = email });
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

        private string GetToken(string sub, string email, bool isAdmin, IEnumerable<string> roles)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim(ClaimTypes.Email, email),
            };
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            else
                claims.AddRange(roles.Select(v => new Claim(ClaimTypes.Role, v)));

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
}
