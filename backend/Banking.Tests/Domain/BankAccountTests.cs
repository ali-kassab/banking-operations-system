using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Tests.Domain;

public class BankAccountTests
{
    [Fact]
    public void Deposit_ValidAmount_IncreasesBalance()
    {
        var account = new BankAccount();

        account.Deposit(150.25m);
        account.Deposit(49.75m);

        Assert.Equal(200m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Deposit_ZeroOrNegativeAmount_Throws(decimal amount)
    {
        var account = new BankAccount();

        Assert.Throws<DomainException>(() => account.Deposit(amount));
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Deposit_MoreThanTwoDecimalPlaces_Throws()
    {
        var account = new BankAccount();

        Assert.Throws<DomainException>(() => account.Deposit(10.005m));
    }

    [Fact]
    public void Deposit_AboveSingleOperationLimit_Throws()
    {
        var account = new BankAccount();

        Assert.Throws<DomainException>(() => account.Deposit(BankAccount.MaxOperationAmount + 0.01m));
    }

    [Fact]
    public void Withdraw_ValidAmount_DecreasesBalance()
    {
        var account = new BankAccount();
        account.Deposit(500m);

        account.Withdraw(120.50m);

        Assert.Equal(379.50m, account.Balance);
    }

    [Fact]
    public void Withdraw_EntireBalance_LeavesZero()
    {
        var account = new BankAccount();
        account.Deposit(300m);

        account.Withdraw(300m);

        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ThrowsAndKeepsBalance()
    {
        var account = new BankAccount();
        account.Deposit(100m);

        var ex = Assert.Throws<DomainException>(() => account.Withdraw(100.01m));

        Assert.Contains("Insufficient funds", ex.Message);
        Assert.Equal(100m, account.Balance); // balance never goes negative
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Withdraw_ZeroOrNegativeAmount_Throws(decimal amount)
    {
        var account = new BankAccount();
        account.Deposit(100m);

        Assert.Throws<DomainException>(() => account.Withdraw(amount));
        Assert.Equal(100m, account.Balance);
    }
}
