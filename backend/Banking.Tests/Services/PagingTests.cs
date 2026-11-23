using System.ComponentModel.DataAnnotations;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests.Services;

public class PagingTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static CustomerService CreateCustomerService(BankingDbContext db) =>
        new(new CustomerRepository(db), new UnitOfWork(db), NullLogger<CustomerService>.Instance);

    private static AccountService CreateAccountService(BankingDbContext db) =>
        new(new BankAccountRepository(db), new CustomerRepository(db), new TransactionRepository(db),
            new UnitOfWork(db), NullLogger<AccountService>.Instance);

    [Fact]
    public async Task Accounts_SecondPage_ReturnsRemainingRowsAndTotals()
    {
        for (var i = 1; i <= 5; i++)
        {
            _database.AddAccount($"100000000{i}", 0m);
        }

        await using var db = _database.CreateContext();
        var page = await CreateAccountService(db).GetPageAsync(null, new PageQuery { Page = 2, PageSize = 2 });

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(["1000000003", "1000000004"], page.Items.Select(a => a.AccountNumber));
    }

    [Fact]
    public async Task Accounts_PageAfterTheLast_IsEmptyButKeepsTheTotal()
    {
        _database.AddAccount("1000000001", 0m);

        await using var db = _database.CreateContext();
        var page = await CreateAccountService(db).GetPageAsync(null, new PageQuery { Page = 3, PageSize = 20 });

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Customers_SearchIsAppliedBeforePaging()
    {
        _database.AddAccount("1000000001", 0m); // customer "Test 1000000001"
        _database.AddAccount("2000000002", 0m);
        _database.AddAccount("2000000003", 0m);

        await using var db = _database.CreateContext();
        var page = await CreateCustomerService(db).GetPageAsync("2000", new PageQuery { Page = 1, PageSize = 1 });

        Assert.Equal(2, page.TotalCount); // only the two matching customers are counted
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].AccountCount);
    }

    [Fact]
    public async Task Transactions_AreNewestFirstAcrossPages()
    {
        var accountId = _database.AddAccount("1000000001", 0m);
        await using var db = _database.CreateContext();
        var service = TestDatabase.CreateTransactionService(db);
        for (var i = 1; i <= 3; i++)
        {
            await service.DepositAsync(accountId, new AmountRequest { Amount = i }, "tester");
        }

        var first = await service.GetAllAsync(new PageQuery { Page = 1, PageSize = 2 });
        var second = await service.GetAllAsync(new PageQuery { Page = 2, PageSize = 2 });

        Assert.Equal(3, first.TotalCount);
        Assert.Equal([3m, 2m], first.Items.Select(t => t.Amount));
        Assert.Equal([1m], second.Items.Select(t => t.Amount));
    }

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, PageQuery.MaxPageSize + 1, false)]
    [InlineData(1, PageQuery.MaxPageSize, true)]
    public void PageQuery_Validation(int page, int pageSize, bool valid)
    {
        var query = new PageQuery { Page = page, PageSize = pageSize };

        var isValid = Validator.TryValidateObject(query, new ValidationContext(query), [], validateAllProperties: true);

        Assert.Equal(valid, isValid);
    }

    [Fact]
    public void PagedResponse_TotalPages_RoundsUp()
    {
        Assert.Equal(0, new PagedResponse<int>([], 1, 20, 0).TotalPages);
        Assert.Equal(1, new PagedResponse<int>([], 1, 20, 20).TotalPages);
        Assert.Equal(2, new PagedResponse<int>([], 1, 20, 21).TotalPages);
    }
}
