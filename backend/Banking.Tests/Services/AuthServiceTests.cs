using System.IdentityModel.Tokens.Jwt;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Repositories;
using Banking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PasswordHasher _hasher = new();

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

    private AuthService CreateService(BankingDbContext db) => TestDatabase.CreateAuthService(db, _hasher);

    private static LoginRequest ValidLogin => new() { Username = "employee", Password = "Correct@123" };

    // ---------- Login ----------

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenWithRoleClaimAndRefreshToken()
    {
        await using var db = _database.CreateContext();

        var result = await CreateService(db).LoginAsync(ValidLogin);

        Assert.NotNull(result);
        Assert.Equal("Employee", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.True(result.RefreshTokenExpiresAt > result.ExpiresAt);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains(token.Claims, c => c.Type == JwtTokenService.RoleClaim && c.Value == "Employee");
        Assert.Contains(token.Claims, c => c.Type == JwtTokenService.NameClaim && c.Value == "employee");
    }

    [Fact]
    public async Task Login_StoresOnlyTheHashOfTheRefreshToken()
    {
        await using var db = _database.CreateContext();

        var result = await CreateService(db).LoginAsync(ValidLogin);

        await using var check = _database.CreateContext();
        var stored = await check.RefreshTokens.SingleAsync();
        Assert.NotEqual(result!.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Login_UsernameIsCaseInsensitive()
    {
        await using var db = _database.CreateContext();

        var result = await CreateService(db).LoginAsync(new LoginRequest { Username = " Employee ", Password = "Correct@123" });

        Assert.NotNull(result);
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
    public async Task Login_DeactivatedUser_ReturnsNull()
    {
        await using (var setup = _database.CreateContext())
        {
            (await setup.Users.SingleAsync()).IsActive = false;
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();
        Assert.Null(await CreateService(db).LoginAsync(ValidLogin));
    }

    // ---------- Refresh ----------

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewPairAndRevokesTheOldToken()
    {
        await using var db = _database.CreateContext();
        var service = CreateService(db);
        var login = await service.LoginAsync(ValidLogin);

        var refreshed = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = login!.RefreshToken });

        Assert.NotNull(refreshed);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal("employee", refreshed.Username);

        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.RefreshTokens.CountAsync(t => t.RevokedAt == null));
    }

    [Fact]
    public async Task Refresh_ReusedToken_IsRejectedAndRevokesAllSessions()
    {
        await using var db = _database.CreateContext();
        var service = CreateService(db);
        var login = await service.LoginAsync(ValidLogin);
        var refreshed = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = login!.RefreshToken });

        // The first token was already used. Presenting it again looks like theft.
        var reused = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken });
        Assert.Null(reused);

        // ...so even the newest token no longer works.
        var afterReuse = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshed!.RefreshToken });
        Assert.Null(afterReuse);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsNull()
    {
        await using var db = _database.CreateContext();

        Assert.Null(await CreateService(db).RefreshAsync(new RefreshTokenRequest { RefreshToken = "not-a-real-token" }));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsNull()
    {
        await using var db = _database.CreateContext();
        var service = CreateService(db);
        var login = await service.LoginAsync(ValidLogin);

        await using (var setup = _database.CreateContext())
        {
            (await setup.RefreshTokens.SingleAsync()).ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await setup.SaveChangesAsync();
        }

        await using var fresh = _database.CreateContext();
        Assert.Null(await CreateService(fresh).RefreshAsync(new RefreshTokenRequest { RefreshToken = login!.RefreshToken }));
    }

    // ---------- Logout ----------

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        await using var db = _database.CreateContext();
        var service = CreateService(db);
        var login = await service.LoginAsync(ValidLogin);

        await service.LogoutAsync(new RefreshTokenRequest { RefreshToken = login!.RefreshToken });

        Assert.Null(await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken }));
    }

    [Fact]
    public async Task Logout_UnknownToken_DoesNothing()
    {
        await using var db = _database.CreateContext();

        await CreateService(db).LogoutAsync(new RefreshTokenRequest { RefreshToken = "unknown" });
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
