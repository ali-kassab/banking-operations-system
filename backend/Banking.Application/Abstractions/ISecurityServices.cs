using Banking.Domain.Entities;

namespace Banking.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public interface ITokenService
{
    /// <summary>Creates a signed JWT for the user and returns it with its expiry time (UTC).</summary>
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
