using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <summary>
/// Sign-in, session refresh and sign-out.
/// Refresh tokens are single-use ("rotation"): each refresh revokes the old token and issues a new one.
/// If an already-used token is presented again it was probably stolen, so all of that user's sessions are revoked.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Returns tokens for valid credentials, or null for a wrong username/password or a deactivated user.</summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username.Trim().ToLowerInvariant(), ct);

        // Same result for "unknown user", "wrong password" and "deactivated" so callers can't discover valid usernames.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password) || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            return null;
        }

        var response = IssueTokens(user);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User {Username} signed in", user.Username);
        return response;
    }

    /// <summary>Exchanges a valid refresh token for a new token pair, or returns null.</summary>
    public async Task<LoginResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var stored = await _refreshTokens.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), ct);
        if (stored?.User is null)
        {
            return null;
        }

        if (stored.RevokedAt is not null)
        {
            // A used (rotated) token came back: someone else may hold a copy, so end every session of this user.
            _logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await RevokeAllAsync(stored.UserId, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return null;
        }

        if (stored.ExpiresAt <= now || !stored.User.IsActive)
        {
            return null;
        }

        stored.Revoke(now);
        var response = IssueTokens(stored.User);
        await _unitOfWork.SaveChangesAsync(ct);
        return response;
    }

    /// <summary>Revokes the given refresh token. Unknown or already revoked tokens are ignored.</summary>
    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _refreshTokens.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), ct);
        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        stored.Revoke(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} signed out", stored.UserId);
    }

    /// <summary>Revokes all sessions of a user. Only stages the change; the caller saves.</summary>
    public async Task RevokeAllAsync(int userId, DateTime nowUtc, CancellationToken ct = default)
    {
        foreach (var token in await _refreshTokens.GetUnrevokedForUserAsync(userId, ct))
        {
            token.Revoke(nowUtc);
        }
    }

    private LoginResponse IssueTokens(User user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user);
        var (refreshToken, refreshHash, refreshExpiresAt) = _tokenService.CreateRefreshToken();

        _refreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = refreshHash, ExpiresAt = refreshExpiresAt });

        return new LoginResponse(token, expiresAt, refreshToken, refreshExpiresAt,
            user.Username, user.FullName, user.Role.ToString());
    }
}
