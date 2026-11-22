using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Banking.Infrastructure.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All dates are stored in UTC. SQL Server's datetime2 doesn't remember that, so mark
        // values read back as UTC; the JSON then ends with "Z" and browsers convert to local time correctly.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(toDb => toDb, fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(c => c.LastName).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(100).IsRequired();
            entity.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(c => c.NationalId).HasMaxLength(20).IsRequired();
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.NationalId).IsUnique();
            entity.Ignore(c => c.FullName);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.Property(a => a.AccountNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

            // Balance is a concurrency token: EF adds "WHERE Balance = <value we read>" to the UPDATE.
            // If another request changed the balance in the meantime, the update affects 0 rows and
            // EF throws DbUpdateConcurrencyException instead of silently overwriting the other change.
            entity.Property(a => a.Balance).HasPrecision(18, 2).IsConcurrencyToken();

            // Restrict: a customer that still has accounts cannot be deleted by accident.
            entity.HasOne(a => a.Customer)
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // The database itself also refuses a negative balance.
            entity.ToTable(t => t.HasCheckConstraint("CK_BankAccounts_Balance_NonNegative", "[Balance] >= 0"));
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.BalanceAfter).HasPrecision(18, 2);
            entity.Property(t => t.Description).HasMaxLength(200);
            entity.Property(t => t.ReferenceNumber).HasMaxLength(30).IsRequired();
            entity.Property(t => t.PerformedBy).HasMaxLength(50);
            entity.HasIndex(t => t.ReferenceNumber);
            entity.HasIndex(t => t.CreatedAt);

            entity.HasOne(t => t.BankAccount)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_Transactions_Amount_Positive", "[Amount] > 0"));
        });
    }
}
