namespace Banking.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Saves all staged changes in one go.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit database transaction,
    /// saves the changes and commits. If anything throws, the transaction is
    /// rolled back and nothing is written.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
}
