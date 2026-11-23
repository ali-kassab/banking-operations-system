using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

public class LoginRequest
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Body of POST /api/auth/refresh and POST /api/auth/logout.</summary>
public class RefreshTokenRequest
{
    [Required, MaxLength(200)]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Returned by login and refresh. <see cref="Token"/> is the short-lived JWT sent with every request;
/// <see cref="RefreshToken"/> is single-use and only sent to /api/auth/refresh to get a new pair.
/// </summary>
public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string Username,
    string FullName,
    string Role);
