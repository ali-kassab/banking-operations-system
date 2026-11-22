using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

/// <summary>
/// One entry in an account's history. A transfer creates two entries:
/// a TransferOut on the source account and a TransferIn on the destination,
/// both sharing the same ReferenceNumber.
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>Always a positive number; the Type tells whether money came in or went out.</summary>
    public decimal Amount { get; set; }

    /// <summary>The account balance right after this transaction was applied.</summary>
    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>For transfers: the other account involved. Null for deposits and withdrawals.</summary>
    public int? CounterpartyAccountId { get; set; }

    public string PerformedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
