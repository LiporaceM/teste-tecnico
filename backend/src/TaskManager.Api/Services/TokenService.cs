using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Models;
using TaskManager.Api.Options;

namespace TaskManager.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(ApplicationUser user, string role);
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public (string Token, DateTime ExpiresAt) Create(ApplicationUser user, string role)
    {
        var o = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Id),
                new Claim("email", user.Email ?? string.Empty),
                new Claim("name", user.FullName),
                new Claim("role", role)
            ]),
            Expires = expires,
            Issuer = o.Issuer,
            Audience = o.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
