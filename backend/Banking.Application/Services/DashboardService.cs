using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Mapping;

namespace Banking.Application.Services;

public class DashboardService
{
    private readonly ICustomerRepository _customers;
    private readonly IBankAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public DashboardService(ICustomerRepository customers, IBankAccountRepository accounts, ITransactionRepository transactions)
    {
        _customers = customers;
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        // DbContext is not thread-safe, so these queries run one after another (not with Task.WhenAll).
        var totalCustomers = await _customers.CountAsync(ct);
        var totalAccounts = await _accounts.CountAsync(ct);
        var totalBalance = await _accounts.GetTotalBalanceAsync(ct);
        var transactionsToday = await _transactions.CountSinceAsync(DateTime.UtcNow.Date, ct);
        var recent = await _transactions.GetRecentAsync(5, ct);

        return new DashboardSummaryResponse(
            totalCustomers,
            totalAccounts,
            totalBalance,
            transactionsToday,
            recent.Select(t => t.ToResponse()).ToList());
    }
}
