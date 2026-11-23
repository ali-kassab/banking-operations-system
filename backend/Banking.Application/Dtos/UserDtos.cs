using System.ComponentModel.DataAnnotations;
using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

public class CreateUserRequest
{
    [Required, RegularExpression(@"^[A-Za-z0-9._-]{3,50}$",
        ErrorMessage = "Username must be 3-50 characters: letters, digits, '.', '_' or '-'.")]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string Password { get; set; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; } = UserRole.Employee;
}

public class ResetPasswordRequest
{
    [Required, RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public record UserResponse(int Id, string Username, string FullName, string Role, bool IsActive, DateTime CreatedAt);

/// <summary>Minimum password policy for staff accounts: 8-100 characters, at least one letter and one digit.</summary>
public static class PasswordRules
{
    public const string Pattern = @"^(?=.*[A-Za-z])(?=.*\d).{8,100}$";
    public const string Message = "Password must be 8-100 characters and contain at least one letter and one digit.";
}
