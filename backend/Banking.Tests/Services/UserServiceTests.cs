using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PasswordHasher _hasher = new();
    private readonly int _adminId;

    public UserServiceTests()
    {
        using var db = _database.CreateContext();
        var admin = new User { Username = "admin", FullName = "Admin", PasswordHash = _hasher.Hash("Admin@123"), Role = UserRole.Admin };
        db.Users.Add(admin);
        db.SaveChanges();
        _adminId = admin.Id;
    }

    public void Dispose() => _database.Dispose();

    private static CreateUserRequest NewUser(string username = "Teller.One") => new()
    {
        Username = username,
        FullName = "  Teller One ",
        Password = "Teller123",
        Role = UserRole.Employee
    };

    [Fact]
    public async Task Create_SavesHashedPasswordAndNormalizesUsername()
    {
        await using var db = _database.CreateContext();

        var created = await TestDatabase.CreateUserService(db, _hasher).CreateAsync(NewUser());

        Assert.Equal("teller.one", created.Username);
        Assert.Equal("Teller One", created.FullName);
        Assert.Equal("Employee", created.Role);
        Assert.True(created.IsActive);

        await using var check = _database.CreateContext();
        var stored = await check.Users.SingleAsync(u => u.Id == created.Id);
        Assert.NotEqual("Teller123", stored.PasswordHash);
        Assert.True(_hasher.Verify(stored.PasswordHash, "Teller123"));
    }

    [Fact]
    public async Task Create_NewUserCanSignIn()
    {
        await using var db = _database.CreateContext();
        await TestDatabase.CreateUserService(db, _hasher).CreateAsync(NewUser());

        var login = await TestDatabase.CreateAuthService(db, _hasher)
            .LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" });

        Assert.NotNull(login);
    }

    [Fact]
    public async Task Create_DuplicateUsername_ThrowsConflict()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<ConflictException>(() =>
            TestDatabase.CreateUserService(db, _hasher).CreateAsync(NewUser("ADMIN")));
    }

    [Fact]
    public async Task Deactivate_BlocksLoginAndRevokesSessions()
    {
        await using var db = _database.CreateContext();
        var users = TestDatabase.CreateUserService(db, _hasher);
        var auth = TestDatabase.CreateAuthService(db, _hasher);
        var teller = await users.CreateAsync(NewUser());
        var session = await auth.LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" });

        var result = await users.SetActiveAsync(teller.Id, false, "admin");

        Assert.False(result.IsActive);
        Assert.Null(await auth.LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" }));
        Assert.Null(await auth.RefreshAsync(new RefreshTokenRequest { RefreshToken = session!.RefreshToken }));
    }

    [Fact]
    public async Task Deactivate_OwnAccount_IsRejected()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<DomainException>(() =>
            TestDatabase.CreateUserService(db, _hasher).SetActiveAsync(_adminId, false, "admin"));
    }

    [Fact]
    public async Task Reactivate_AllowsLoginAgain()
    {
        await using var db = _database.CreateContext();
        var users = TestDatabase.CreateUserService(db, _hasher);
        var teller = await users.CreateAsync(NewUser());
        await users.SetActiveAsync(teller.Id, false, "admin");

        await users.SetActiveAsync(teller.Id, true, "admin");

        Assert.NotNull(await TestDatabase.CreateAuthService(db, _hasher)
            .LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" }));
    }

    [Fact]
    public async Task ResetPassword_OldPasswordStopsWorkingAndSessionsEnd()
    {
        await using var db = _database.CreateContext();
        var users = TestDatabase.CreateUserService(db, _hasher);
        var auth = TestDatabase.CreateAuthService(db, _hasher);
        var teller = await users.CreateAsync(NewUser());
        var session = await auth.LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" });

        await users.ResetPasswordAsync(teller.Id, new ResetPasswordRequest { NewPassword = "NewPass456" });

        Assert.Null(await auth.LoginAsync(new LoginRequest { Username = "teller.one", Password = "Teller123" }));
        Assert.NotNull(await auth.LoginAsync(new LoginRequest { Username = "teller.one", Password = "NewPass456" }));
        Assert.Null(await auth.RefreshAsync(new RefreshTokenRequest { RefreshToken = session!.RefreshToken }));
    }

    [Fact]
    public async Task UnknownUser_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var users = TestDatabase.CreateUserService(db, _hasher);

        await Assert.ThrowsAsync<NotFoundException>(() => users.SetActiveAsync(999, false, "admin"));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            users.ResetPasswordAsync(999, new ResetPasswordRequest { NewPassword = "NewPass456" }));
    }

    [Theory]
    [InlineData("short1", false)]       // too short
    [InlineData("onlyletters", false)]  // no digit
    [InlineData("12345678", false)]     // no letter
    [InlineData("Teller123", true)]
    public void PasswordRules_AreEnforcedByTheRequestValidation(string password, bool valid)
    {
        var request = new ResetPasswordRequest { NewPassword = password };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request, new System.ComponentModel.DataAnnotations.ValidationContext(request), results, validateAllProperties: true);

        Assert.Equal(valid, isValid);
    }
}
