using Banking.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Banking.Application.Abstractions.IPasswordHasher;

namespace Banking.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's password hasher (PBKDF2 with a random salt per password).
/// We use only the hasher, not the full Identity system, to keep things small.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
