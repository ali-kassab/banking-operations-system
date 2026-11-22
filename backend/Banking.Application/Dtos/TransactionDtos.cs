using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>Body for deposit and withdrawal requests.</summary>
public class AmountRequest
{
    // The Range check gives a quick 400 for obvious mistakes;
    // BankAccount.ValidateAmount is the real rule and is always enforced.
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

public class TransferRequest
{
    [Range(1, int.MaxValue)]
    public int FromAccountId { get; set; }

    [Range(1, int.MaxValue)]
    public int ToAccountId { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

public record TransactionResponse(
    int Id,
    int AccountId,
    string AccountNumber,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string Description,
    string ReferenceNumber,
    int? CounterpartyAccountId,
    string PerformedBy,
    DateTime CreatedAt);

public record TransferResponse(
    string ReferenceNumber,
    decimal Amount,
    AccountResponse FromAccount,
    AccountResponse ToAccount);
