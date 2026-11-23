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

    /// <summary>Creates a random refresh token. <c>Token</c> goes to the client; only <c>Hash</c> is stored.</summary>
    (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken();

    /// <summary>Hashes a refresh token received from a client so it can be looked up.</summary>
    string HashRefreshToken(string token);
}
