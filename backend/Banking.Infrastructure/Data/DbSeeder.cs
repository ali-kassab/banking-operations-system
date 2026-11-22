using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Data;

/// <summary>
/// Inserts demo users and sample data into an empty database.
/// Only called in the Development environment (see Program.cs).
/// </summary>
public static class DbSeeder
{
    // Development-only sample credentials (documented in the README).
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin@123";
    public const string EmployeeUsername = "employee";
    public const string EmployeePassword = "Employee@123";

    public static async Task SeedAsync(BankingDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Users.AnyAsync())
        {
            return; // already seeded
        }

        db.Users.AddRange(
            new User
            {
                Username = AdminUsername,
                FullName = "System Administrator",
                PasswordHash = passwordHasher.Hash(AdminPassword),
                Role = UserRole.Admin
            },
            new User
            {
                Username = EmployeeUsername,
                FullName = "Bank Employee",
                PasswordHash = passwordHasher.Hash(EmployeePassword),
                Role = UserRole.Employee
            });

        AddCustomerWithAccount(db, "Sara", "Haddad", "sara.haddad@example.com", "+961-70-111-222", "NID-1001", "1000000001", AccountType.Checking, 2500m);
        AddCustomerWithAccount(db, "Omar", "Khalil", "omar.khalil@example.com", "+961-71-333-444", "NID-1002", "1000000002", AccountType.Savings, 12000m);
        AddCustomerWithAccount(db, "Lina", "Mansour", "lina.mansour@example.com", "+961-76-555-666", "NID-1003", "1000000003", AccountType.Checking, 800m);

        await db.SaveChangesAsync();
    }

    private static void AddCustomerWithAccount(BankingDbContext db, string firstName, string lastName,
        string email, string phone, string nationalId, string accountNumber, AccountType type, decimal openingBalance)
    {
        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phone,
            NationalId = nationalId
        };

        var account = new BankAccount { AccountNumber = accountNumber, Type = type, Customer = customer };
        account.Deposit(openingBalance);

        db.Customers.Add(customer);
        db.BankAccounts.Add(account);
        db.Transactions.Add(new Transaction
        {
            BankAccount = account,
            Type = TransactionType.Deposit,
            Amount = openingBalance,
            BalanceAfter = account.Balance,
            Description = "Initial deposit",
            ReferenceNumber = $"SEED-{accountNumber}",
            PerformedBy = "seed"
        });
    }
}
