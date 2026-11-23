using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <summary>Staff account management. Every endpoint that uses this service is Admin-only.</summary>
public class UserService
{
    private readonly IUserRepository _users;
    private readonly AuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository users,
        AuthService authService,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger)
    {
        _users = users;
        _authService = authService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PagedResponse<UserResponse>> GetPageAsync(PageQuery page, CancellationToken ct = default)
    {
        var (items, total) = await _users.GetPageAsync(page.Skip, page.PageSize, ct);
        return new PagedResponse<UserResponse>(items.Select(u => u.ToResponse()).ToList(), page.Page, page.PageSize, total);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await _users.UsernameExistsAsync(username, ct))
        {
            throw new ConflictException($"A user with username '{username}' already exists.");
        }

        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role
        };

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created user {Username} with role {Role}", user.Username, user.Role);
        return user.ToResponse();
    }

    /// <summary>Activates or deactivates a user. Deactivation also ends all of the user's sessions.</summary>
    public async Task<UserResponse> SetActiveAsync(int id, bool isActive, string currentUsername, CancellationToken ct = default)
    {
        var user = await GetOrThrowAsync(id, ct);

        // Prevents an admin from locking themselves (and possibly everyone) out.
        if (!isActive && string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("You cannot deactivate your own account.");
        }

        user.IsActive = isActive;
        if (!isActive)
        {
            await _authService.RevokeAllAsync(user.Id, DateTime.UtcNow, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User {Username} was {Status}", user.Username, isActive ? "activated" : "deactivated");
        return user.ToResponse();
    }

    /// <summary>Sets a new password and ends all of the user's sessions.</summary>
    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await GetOrThrowAsync(id, ct);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _authService.RevokeAllAsync(user.Id, DateTime.UtcNow, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Password reset for user {Username}", user.Username);
    }

    private async Task<User> GetOrThrowAsync(int id, CancellationToken ct) =>
        await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException($"User {id} was not found.");
}
