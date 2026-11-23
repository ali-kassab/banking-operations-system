using Banking.Domain.Entities;

namespace Banking.Application.Abstractions;

// The Application layer describes WHAT data it needs through these small,
// entity-specific interfaces. The Infrastructure layer decides HOW (EF Core + SQL Server).
// Repositories only stage changes; IUnitOfWork saves them.
// List methods return one page of rows plus the total number of matching rows.

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Loads a user tracked, so callers may change and save it.</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(List<User> Items, int TotalCount)> GetPageAsync(int skip, int take, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    void Add(User user);
}

public interface IRefreshTokenRepository
{
    /// <summary>Loads a refresh token (with its user) by the SHA-256 hash of its value.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>All tokens of a user that are not revoked yet (expired ones included).</summary>
    Task<List<RefreshToken>> GetUnrevokedForUserAsync(int userId, CancellationToken ct = default);

    void Add(RefreshToken token);
}

public interface ICustomerRepository
{
    Task<(List<Customer> Items, int TotalCount)> GetPageAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    void Add(Customer customer);
}

public interface IBankAccountRepository
{
    Task<(List<BankAccount> Items, int TotalCount)> GetPageAsync(int? customerId, int skip, int take, CancellationToken ct = default);

    /// <summary>Loads an account (with its customer) so it can be read or modified.</summary>
    Task<BankAccount?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);
    void Add(BankAccount account);
}

public interface ITransactionRepository
{
    /// <summary>Newest first. <paramref name="accountId"/> null means all accounts.</summary>
    Task<(List<Transaction> Items, int TotalCount)> GetPageAsync(int? accountId, int skip, int take, CancellationToken ct = default);

    Task<List<Transaction>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<int> CountSinceAsync(DateTime fromUtc, CancellationToken ct = default);
    void Add(Transaction transaction);
}
