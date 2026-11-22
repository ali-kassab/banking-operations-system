using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Entities;

/// <summary>
/// A customer's bank account. The balance can only be changed through
/// Deposit and Withdraw, so the balance rules live in exactly one place.
/// </summary>
public class BankAccount
{
    /// <summary>Upper limit for a single operation. Keeps typos like 1000000 instead of 1000 out.</summary>
    public const decimal MaxOperationAmount = 1_000_000m;

    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public List<Transaction> Transactions { get; set; } = new();

    public void Deposit(decimal amount)
    {
        ValidateAmount(amount);
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        ValidateAmount(amount);

        if (amount > Balance)
        {
            throw new DomainException(
                $"Insufficient funds. Available balance is {Balance:0.00}, requested {amount:0.00}.");
        }

        Balance -= amount;
    }

    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        if (amount > MaxOperationAmount)
        {
            throw new DomainException($"Amount cannot exceed {MaxOperationAmount:0.00} in a single operation.");
        }

        // Money is stored with 2 decimal places, so 10.005 is not a valid amount.
        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Amount cannot have more than 2 decimal places.");
        }
    }
}
