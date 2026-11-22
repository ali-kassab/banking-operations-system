using System.IdentityModel.Tokens.Jwt;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Repositories;
using Banking.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PasswordHasher _hasher = new();
    private readonly JwtSettings _jwt = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "unit-test-signing-key-that-is-long-enough-123",
        ExpiryMinutes = 30
    };

    public AuthServiceTests()
    {
        using var db = _database.CreateContext();
        db.Users.Add(new User
        {
            Username = "employee",
            FullName = "Test Employee",
            PasswordHash = _hasher.Hash("Correct@123"),
            Role = UserRole.Employee
        });
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private AuthService CreateService(Banking.Infrastructure.Data.BankingDbContext db) =>
        new(new UserRepository(db), _hasher, new JwtTokenService(_jwt), NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenWithRoleClaim()
    {
        await using var db = _database.CreateContext();

        var result = await CreateService(db).LoginAsync(new LoginRequest { Username = "employee", Password = "Correct@123" });

        Assert.NotNull(result);
        Assert.Equal("Employee", result.Role);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains(token.Claims, c => c.Type == JwtTokenService.RoleClaim && c.Value == "Employee");
        Assert.Contains(token.Claims, c => c.Type == JwtTokenService.NameClaim && c.Value == "employee");
    }

    [Theory]
    [InlineData("employee", "wrong-password")]
    [InlineData("nobody", "Correct@123")]
    public async Task Login_InvalidCredentials_ReturnsNull(string username, string password)
    {
        await using var db = _database.CreateContext();

        var result = await CreateService(db).LoginAsync(new LoginRequest { Username = username, Password = password });

        Assert.Null(result);
    }

    [Fact]
    public void PasswordHasher_NeverStoresPlainText()
    {
        var hash = _hasher.Hash("Secret@123");

        Assert.NotEqual("Secret@123", hash);
        Assert.True(_hasher.Verify(hash, "Secret@123"));
        Assert.False(_hasher.Verify(hash, "secret@123"));
    }
}
