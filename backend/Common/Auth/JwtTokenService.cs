using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gmao.Api.Common.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Gmao.Api.Common.Auth;

public interface IJwtTokenService
{
    string CreateToken(int userId, string username, string fullName, string roleCode);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public string CreateToken(int userId, string username, string fullName, string roleCode)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("full_name", fullName),
            new(ClaimTypes.Role, roleCode)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
