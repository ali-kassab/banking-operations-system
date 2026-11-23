using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

/// <summary>
/// A bank employee who can sign in to the system (not a bank customer).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    /// <summary>
    /// Deactivated users cannot sign in or refresh their session. Users are never deleted,
    /// because their username stays in the transaction history ("PerformedBy").
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
