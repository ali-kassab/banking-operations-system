using Banking.Application.Dtos;
using Banking.Domain.Entities;

namespace Banking.Application.Mapping;

/// <summary>
/// Hand-written entity → DTO conversions. Kept in one place so every endpoint
/// returns the same shape, without needing a mapping library.
/// </summary>
public static class DtoMapper
{
    public static CustomerResponse ToResponse(this Customer customer) => new(
        customer.Id,
        customer.FirstName,
        customer.LastName,
        customer.FullName,
        customer.Email,
        customer.PhoneNumber,
        customer.NationalId,
        customer.Accounts.Count,
        customer.CreatedAt);

    public static AccountResponse ToResponse(this BankAccount account) => new(
        account.Id,
        account.AccountNumber,
        account.Type.ToString(),
        account.Balance,
        account.CustomerId,
        account.Customer?.FullName ?? string.Empty,
        account.CreatedAt);

    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt);

    public static TransactionResponse ToResponse(this Transaction transaction) => new(
        transaction.Id,
        transaction.BankAccountId,
        transaction.BankAccount?.AccountNumber ?? string.Empty,
        transaction.Type.ToString(),
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.Description,
        transaction.ReferenceNumber,
        transaction.CounterpartyAccountId,
        transaction.PerformedBy,
        transaction.CreatedAt);
}
