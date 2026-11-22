using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

public class CreateCustomerRequest
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string NationalId { get; set; } = string.Empty;
}

public record CustomerResponse(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string NationalId,
    int AccountCount,
    DateTime CreatedAt);
