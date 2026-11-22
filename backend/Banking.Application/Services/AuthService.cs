using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Returns a token for valid credentials, or null when the username or password is wrong.</summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username.Trim(), ct);

        // Same result for "unknown user" and "wrong password" so callers can't discover valid usernames.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            return null;
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);
        _logger.LogInformation("User {Username} signed in", user.Username);

        return new LoginResponse(token, expiresAt, user.Username, user.FullName, user.Role.ToString());
    }
}
