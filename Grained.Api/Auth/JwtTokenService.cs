using System.Security.Claims;
using System.Text;
using Grained.Domain.Entities;
using Grained.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grained.Api.Auth;

// Issues signed JWTs carrying the same claims the Blazor cookie principal carried
// (user id, roles, ChurchId, full name) so the shared ICurrentUserService logic and
// church-isolation checks keep working unchanged behind the API.
public class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public (string token, DateTime expiresAtUtc) Create(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ApplicationUserClaimsPrincipalFactory.FullNameClaimType, user.FullName),
        };
        if (user.ChurchId is not null)
        {
            claims.Add(new Claim(ApplicationUserClaimsPrincipalFactory.ChurchIdClaimType, user.ChurchId.Value.ToString()));
        }
        if (user.MustChangePassword)
        {
            claims.Add(new Claim("must_change_password", "true"));
        }
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            NotBefore = now,
            IssuedAt = now,
            Issuer = _opt.Issuer,
            Audience = _opt.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }
}
