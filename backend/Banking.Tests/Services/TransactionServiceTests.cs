using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private const string User = "tester";
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // ---------- Deposits and withdrawals ----------

    [Fact]
    public async Task Deposit_UpdatesBalanceAndRecordsTransaction()
    {
        var accountId = _database.AddAccount("1000000001", 100m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        var result = await service.DepositAsync(accountId, new AmountRequest { Amount = 50m }, User);

        Assert.Equal("Deposit", result.Type);
        Assert.Equal(150m, result.BalanceAfter);
        Assert.Equal(User, result.PerformedBy);
        Assert.Equal(150m, _database.GetBalance(accountId));
        Assert.Equal(1, _database.CountTransactions());
    }

    [Fact]
    public async Task Withdraw_UpdatesBalanceAndRecordsTransaction()
    {
        var accountId = _database.AddAccount("1000000001", 100m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        var result = await service.WithdrawAsync(accountId, new AmountRequest { Amount = 30m }, User);

        Assert.Equal("Withdrawal", result.Type);
        Assert.Equal(70m, result.BalanceAfter);
        Assert.Equal(70m, _database.GetBalance(accountId));
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ThrowsAndSavesNothing()
    {
        var accountId = _database.AddAccount("1000000001", 100m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.WithdrawAsync(accountId, new AmountRequest { Amount = 100.01m }, User));

        Assert.Equal(100m, _database.GetBalance(accountId));
        Assert.Equal(0, _database.CountTransactions());
    }

    [Fact]
    public async Task Deposit_UnknownAccount_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DepositAsync(999, new AmountRequest { Amount = 10m }, User));
    }

    // ---------- Transfers ----------

    [Fact]
    public async Task Transfer_MovesMoneyAndCreatesTwoLinkedTransactions()
    {
        var fromId = _database.AddAccount("1000000001", 1000m);
        var toId = _database.AddAccount("1000000002", 200m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        var result = await service.TransferAsync(
            new TransferRequest { FromAccountId = fromId, ToAccountId = toId, Amount = 250.50m }, User);

        Assert.Equal(749.50m, result.FromAccount.Balance);
        Assert.Equal(450.50m, result.ToAccount.Balance);
        Assert.Equal(749.50m, _database.GetBalance(fromId));
        Assert.Equal(450.50m, _database.GetBalance(toId));

        // The total amount of money in the bank is unchanged by a transfer.
        Assert.Equal(1200m, _database.GetBalance(fromId) + _database.GetBalance(toId));

        await using var check = _database.CreateContext();
        var rows = await check.Transactions.Where(t => t.ReferenceNumber == result.ReferenceNumber).ToListAsync();
        Assert.Equal(2, rows.Count);

        var outRow = rows.Single(t => t.Type == TransactionType.TransferOut);
        var inRow = rows.Single(t => t.Type == TransactionType.TransferIn);
        Assert.Equal(fromId, outRow.BankAccountId);
        Assert.Equal(toId, outRow.CounterpartyAccountId);
        Assert.Equal(toId, inRow.BankAccountId);
        Assert.Equal(fromId, inRow.CounterpartyAccountId);
    }

    [Fact]
    public async Task Transfer_EntireBalance_IsAllowed()
    {
        var fromId = _database.AddAccount("1000000001", 300m);
        var toId = _database.AddAccount("1000000002", 0m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await service.TransferAsync(new TransferRequest { FromAccountId = fromId, ToAccountId = toId, Amount = 300m }, User);

        Assert.Equal(0m, _database.GetBalance(fromId));
        Assert.Equal(300m, _database.GetBalance(toId));
    }

    [Fact]
    public async Task Transfer_ExceedingBalance_ThrowsAndChangesNothing()
    {
        var fromId = _database.AddAccount("1000000001", 100m);
        var toId = _database.AddAccount("1000000002", 50m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.TransferAsync(
            new TransferRequest { FromAccountId = fromId, ToAccountId = toId, Amount = 100.01m }, User));

        Assert.Equal(100m, _database.GetBalance(fromId));
        Assert.Equal(50m, _database.GetBalance(toId));
        Assert.Equal(0, _database.CountTransactions());
    }

    [Fact]
    public async Task Transfer_ToSameAccount_Throws()
    {
        var accountId = _database.AddAccount("1000000001", 100m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.TransferAsync(
            new TransferRequest { FromAccountId = accountId, ToAccountId = accountId, Amount = 10m }, User));

        Assert.Equal(100m, _database.GetBalance(accountId));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.001")]
    [InlineData("1000000.01")]
    public async Task Transfer_InvalidAmount_Throws(string amountText)
    {
        var amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        var fromId = _database.AddAccount("1000000001", 100m);
        var toId = _database.AddAccount("1000000002", 0m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.TransferAsync(
            new TransferRequest { FromAccountId = fromId, ToAccountId = toId, Amount = amount }, User));

        Assert.Equal(100m, _database.GetBalance(fromId));
        Assert.Equal(0m, _database.GetBalance(toId));
    }

    [Fact]
    public async Task Transfer_UnknownDestination_ThrowsNotFoundAndChangesNothing()
    {
        var fromId = _database.AddAccount("1000000001", 100m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.TransferAsync(
            new TransferRequest { FromAccountId = fromId, ToAccountId = 999, Amount = 10m }, User));

        Assert.Equal(100m, _database.GetBalance(fromId));
    }

    [Fact]
    public async Task SequentialOperations_KeepBalanceAndHistoryConsistent()
    {
        var aId = _database.AddAccount("1000000001", 0m);
        var bId = _database.AddAccount("1000000002", 0m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);

        await service.DepositAsync(aId, new AmountRequest { Amount = 1000m }, User);
        await service.WithdrawAsync(aId, new AmountRequest { Amount = 125.25m }, User);
        await service.TransferAsync(new TransferRequest { FromAccountId = aId, ToAccountId = bId, Amount = 400m }, User);
        await service.TransferAsync(new TransferRequest { FromAccountId = bId, ToAccountId = aId, Amount = 100m }, User);

        Assert.Equal(574.75m, _database.GetBalance(aId));
        Assert.Equal(300m, _database.GetBalance(bId));

        var history = await service.GetAccountHistoryAsync(aId);
        Assert.Equal(4, history.Count);
        Assert.Equal(574.75m, history.First().BalanceAfter); // newest first; matches the current balance
    }

    // ---------- Concurrency ----------

    [Fact]
    public async Task ConcurrentWithdrawals_SecondStaleUpdateIsRejected()
    {
        var accountId = _database.AddAccount("1000000001", 100m);

        // Two requests read the same balance (100) at the same time...
        await using var db1 = _database.CreateContext();
        await using var db2 = _database.CreateContext();
        var service1 = TestDatabase.CreateTransactionService(db1);
        var service2 = TestDatabase.CreateTransactionService(db2);
        await db2.BankAccounts.SingleAsync(a => a.Id == accountId); // db2 now tracks the stale value 100

        // ...the first one withdraws 80 and saves (balance is now 20)...
        await service1.WithdrawAsync(accountId, new AmountRequest { Amount = 80m }, User);

        // ...the second one still believes the balance is 100 and also tries to withdraw 80.
        // Balance is a concurrency token, so the stale update is refused instead of creating money.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service2.WithdrawAsync(accountId, new AmountRequest { Amount = 80m }, User));

        Assert.Equal(20m, _database.GetBalance(accountId));
        Assert.Equal(1, _database.CountTransactions());
    }
}
