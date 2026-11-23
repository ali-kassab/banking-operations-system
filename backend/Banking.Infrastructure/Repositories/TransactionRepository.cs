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

    public async Task<(List<Transaction> Items, int TotalCount)> GetPageAsync(int? accountId, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking();

        if (accountId.HasValue)
        {
            query = query.Where(t => t.BankAccountId == accountId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await NewestFirst(query.Include(t => t.BankAccount))
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<List<Transaction>> GetRecentAsync(int count, CancellationToken ct = default) =>
        NewestFirst(_db.Transactions.AsNoTracking().Include(t => t.BankAccount))
            .Take(count)
            .ToListAsync(ct);

    public Task<int> CountSinceAsync(DateTime fromUtc, CancellationToken ct = default) =>
        _db.Transactions.CountAsync(t => t.CreatedAt >= fromUtc, ct);

    public void Add(Transaction transaction) => _db.Transactions.Add(transaction);

    // Id breaks ties between rows written in the same instant (e.g. both halves of a transfer).
    private static IQueryable<Transaction> NewestFirst(IQueryable<Transaction> query) =>
        query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
}
