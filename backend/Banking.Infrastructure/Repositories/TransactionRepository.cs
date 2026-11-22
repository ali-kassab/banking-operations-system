using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly BankingDbContext _db;

    public TransactionRepository(BankingDbContext db)
    {
        _db = db;
    }

    public Task<List<Transaction>> GetByAccountIdAsync(int accountId, CancellationToken ct = default) =>
        _db.Transactions.AsNoTracking()
            .Include(t => t.BankAccount)
            .Where(t => t.BankAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .ToListAsync(ct);

    public Task<List<Transaction>> GetRecentAsync(int count, CancellationToken ct = default) =>
        _db.Transactions.AsNoTracking()
            .Include(t => t.BankAccount)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Take(count)
            .ToListAsync(ct);

    public Task<int> CountSinceAsync(DateTime fromUtc, CancellationToken ct = default) =>
        _db.Transactions.CountAsync(t => t.CreatedAt >= fromUtc, ct);

    public void Add(Transaction transaction) => _db.Transactions.Add(transaction);
}
