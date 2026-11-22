using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Services;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests.Services;

public class AccountAndCustomerServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static CustomerService CreateCustomerService(BankingDbContext db) =>
        new(new CustomerRepository(db), new UnitOfWork(db), NullLogger<CustomerService>.Instance);

    private static AccountService CreateAccountService(BankingDbContext db) =>
        new(new BankAccountRepository(db), new CustomerRepository(db), new TransactionRepository(db),
            new UnitOfWork(db), NullLogger<AccountService>.Instance);

    private static CreateCustomerRequest NewCustomer(string email = "jane@example.com", string nationalId = "NID-1") => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = email,
        PhoneNumber = "+1-555-0100",
        NationalId = nationalId
    };

    [Fact]
    public async Task CreateCustomer_SavesAndNormalizesEmail()
    {
        await using var db = _database.CreateContext();
        var service = CreateCustomerService(db);

        var created = await service.CreateAsync(NewCustomer(email: "  Jane@Example.COM "));

        Assert.True(created.Id > 0);
        Assert.Equal("jane@example.com", created.Email);
        Assert.Equal("Jane Doe", created.FullName);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ThrowsConflict()
    {
        await using var db = _database.CreateContext();
        var service = CreateCustomerService(db);
        await service.CreateAsync(NewCustomer());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(NewCustomer(email: "JANE@example.com", nationalId: "NID-2")));
    }

    [Fact]
    public async Task CreateCustomer_DuplicateNationalId_ThrowsConflict()
    {
        await using var db = _database.CreateContext();
        var service = CreateCustomerService(db);
        await service.CreateAsync(NewCustomer());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(NewCustomer(email: "other@example.com", nationalId: "NID-1")));
    }

    [Fact]
    public async Task CreateAccount_WithInitialDeposit_SetsBalanceAndRecordsDeposit()
    {
        await using var db = _database.CreateContext();
        var customer = await CreateCustomerService(db).CreateAsync(NewCustomer());

        var account = await CreateAccountService(db).CreateAsync(
            new CreateAccountRequest { CustomerId = customer.Id, Type = AccountType.Savings, InitialDeposit = 500m }, "admin");

        Assert.Equal(10, account.AccountNumber.Length);
        Assert.Equal("Savings", account.Type);
        Assert.Equal(500m, account.Balance);
        Assert.Equal("Jane Doe", account.CustomerName);

        await using var check = _database.CreateContext();
        var deposit = await check.Transactions.SingleAsync();
        Assert.Equal(TransactionType.Deposit, deposit.Type);
        Assert.Equal(500m, deposit.BalanceAfter);
    }

    [Fact]
    public async Task CreateAccount_WithoutInitialDeposit_OpensEmptyWithNoHistory()
    {
        await using var db = _database.CreateContext();
        var customer = await CreateCustomerService(db).CreateAsync(NewCustomer());

        var account = await CreateAccountService(db).CreateAsync(
            new CreateAccountRequest { CustomerId = customer.Id }, "admin");

        Assert.Equal(0m, account.Balance);
        Assert.Equal(0, _database.CountTransactions());
    }

    [Fact]
    public async Task CreateAccount_UnknownCustomer_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateAccountService(db).CreateAsync(new CreateAccountRequest { CustomerId = 42 }, "admin"));
    }
}
