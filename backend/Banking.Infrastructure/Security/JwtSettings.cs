namespace Banking.Infrastructure.Security;

/// <summary>Bound from the "Jwt" section of appsettings.</summary>
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC signing key. Must be at least 32 characters. Never commit a real one.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Lifetime of the access token (JWT). Kept short because a JWT cannot be revoked.</summary>
    public int ExpiryMinutes { get; set; } = 15;

    /// <summary>Lifetime of a refresh token. Each use replaces it with a new one.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
