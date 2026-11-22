using System.ComponentModel.DataAnnotations;
using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

public class CreateAccountRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId is required.")]
    public int CustomerId { get; set; }

    [EnumDataType(typeof(AccountType))]
    public AccountType Type { get; set; } = AccountType.Checking;

    /// <summary>Optional first deposit. Zero means the account opens empty.</summary>
    [Range(0, 1_000_000)]
    public decimal InitialDeposit { get; set; }
}

public record AccountResponse(
    int Id,
    string AccountNumber,
    string Type,
    decimal Balance,
    int CustomerId,
    string CustomerName,
    DateTime CreatedAt);
