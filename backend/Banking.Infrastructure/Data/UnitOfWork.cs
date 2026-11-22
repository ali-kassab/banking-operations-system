using Banking.Application.Abstractions;

namespace Banking.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly BankingDbContext _db;

    public UnitOfWork(BankingDbContext db)
    {
        _db = db;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync(ct);

        await operation();
        await _db.SaveChangesAsync(ct);
        await dbTransaction.CommitAsync(ct);

        // If anything above throws, CommitAsync is never reached and disposing
        // the transaction rolls it back automatically.
    }
}
