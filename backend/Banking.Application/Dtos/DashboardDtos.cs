namespace Banking.Application.Dtos;

public record DashboardSummaryResponse(
    int TotalCustomers,
    int TotalAccounts,
    decimal TotalBalance,
    int TransactionsToday,
    List<TransactionResponse> RecentTransactions);
