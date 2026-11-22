using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests;

/// <summary>
/// A throw-away SQLite database that lives in memory for the duration of one test.
/// Unlike the EF "InMemory" provider, SQLite is a real relational database, so
/// transactions, unique indexes and check constraints actually behave like they do in SQL Server.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // the in-memory database exists as long as this connection stays open

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public BankingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BankingDbContext>().UseSqlite(_connection).Options);

    /// <summary>Builds a TransactionService wired to real repositories over the given context.</summary>
    public static TransactionService CreateTransactionService(BankingDbContext db) => new(
        new BankAccountRepository(db),
        new TransactionRepository(db),
        new UnitOfWork(db),
        NullLogger<TransactionService>.Instance);

    /// <summary>Inserts a customer with one account holding <paramref name="balance"/> and returns the account id.</summary>
    public int AddAccount(string accountNumber, decimal balance)
    {
        using var db = CreateContext();

        var customer = new Customer
        {
            FirstName = "Test",
            LastName = accountNumber,
            Email = $"{accountNumber}@test.local",
            PhoneNumber = "000",
            NationalId = accountNumber
        };
        var account = new BankAccount { AccountNumber = accountNumber, Type = AccountType.Checking, Customer = customer };
        if (balance > 0)
        {
            account.Deposit(balance);
        }

        db.BankAccounts.Add(account);
        db.SaveChanges();
        return account.Id;
    }

    public decimal GetBalance(int accountId)
    {
        using var db = CreateContext(); // fresh context = read what is really in the database
        return db.BankAccounts.Single(a => a.Id == accountId).Balance;
    }

    public int CountTransactions()
    {
        using var db = CreateContext();
        return db.Transactions.Count();
    }

    public void Dispose() => _connection.Dispose();
}
