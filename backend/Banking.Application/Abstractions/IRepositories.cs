using Banking.Domain.Entities;

namespace Banking.Application.Abstractions;

// The Application layer describes WHAT data it needs through these small,
// entity-specific interfaces. The Infrastructure layer decides HOW (EF Core + SQL Server).
// Repositories only stage changes; IUnitOfWork saves them.

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(string? search, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    void Add(Customer customer);
}

public interface IBankAccountRepository
{
    Task<List<BankAccount>> GetAllAsync(int? customerId, CancellationToken ct = default);

    /// <summary>Loads an account (with its customer) so it can be read or modified.</summary>
    Task<BankAccount?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);
    void Add(BankAccount account);
}

public interface ITransactionRepository
{
    Task<List<Transaction>> GetByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<List<Transaction>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<int> CountSinceAsync(DateTime fromUtc, CancellationToken ct = default);
    void Add(Transaction transaction);
}
