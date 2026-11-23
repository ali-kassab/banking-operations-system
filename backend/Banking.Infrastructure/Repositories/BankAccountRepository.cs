using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

public class BankAccountRepository : IBankAccountRepository
{
    private readonly BankingDbContext _db;

    public BankAccountRepository(BankingDbContext db)
    {
        _db = db;
    }

    public async Task<(List<BankAccount> Items, int TotalCount)> GetPageAsync(int? customerId, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.BankAccounts.AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(a => a.Customer)
            .OrderBy(a => a.AccountNumber) // unique, so the paging order is stable
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    // Tracked (no AsNoTracking) because callers may change the balance and save.
    public Task<BankAccount?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.BankAccounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken ct = default) =>
        _db.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _db.BankAccounts.CountAsync(ct);

    public async Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default) =>
        await _db.BankAccounts.SumAsync(a => (decimal?)a.Balance, ct) ?? 0m;

    public void Add(BankAccount account) => _db.BankAccounts.Add(account);
}
