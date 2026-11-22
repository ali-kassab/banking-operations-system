using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Banking.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    // Short claim names. The API is configured to read "name" and "role" from these same keys.
    public const string NameClaim = "name";
    public const string RoleClaim = "role";

    private readonly JwtSettings _settings;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(NameClaim, user.Username),
            new Claim(RoleClaim, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
