namespace Banking.Domain.Entities;

/// <summary>
/// A long-lived, single-use token that lets a signed-in user get a new short-lived JWT
/// without typing the password again. Only the SHA-256 hash of the token is stored,
/// so a leaked database does not reveal usable tokens.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the token was used (rotated), on sign-out, or when the user was deactivated.</summary>
    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime nowUtc) => RevokedAt is null && ExpiresAt > nowUtc;

    public void Revoke(DateTime nowUtc) => RevokedAt ??= nowUtc;
}
