using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

    public (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken()
    {
        // 64 random bytes from a cryptographically secure generator, URL-safe Base64 encoded.
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, HashRefreshToken(token), DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    // A plain SHA-256 is enough here (unlike passwords): the token is long and random, so it cannot be guessed.
    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
