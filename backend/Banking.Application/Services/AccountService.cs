using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

public class AccountService
{
    private readonly IBankAccountRepository _accounts;
    private readonly ICustomerRepository _customers;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IBankAccountRepository accounts,
        ICustomerRepository customers,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        ILogger<AccountService> logger)
    {
        _accounts = accounts;
        _customers = customers;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResponse<AccountResponse>> GetPageAsync(int? customerId, PageQuery page, CancellationToken ct = default)
    {
        var (accounts, total) = await _accounts.GetPageAsync(customerId, page.Skip, page.PageSize, ct);
        return new PagedResponse<AccountResponse>(
            accounts.Select(a => a.ToResponse()).ToList(), page.Page, page.PageSize, total);
    }

    public async Task<AccountResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Account {id} was not found.");

        return account.ToResponse();
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, string performedBy, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException($"Customer {request.CustomerId} was not found.");

        var account = new BankAccount
        {
            AccountNumber = await GenerateUniqueAccountNumberAsync(ct),
            Type = request.Type,
            Customer = customer
        };
        _accounts.Add(account);

        // An opening deposit is recorded like any other deposit so the history matches the balance.
        if (request.InitialDeposit > 0)
        {
            account.Deposit(request.InitialDeposit);
            _transactions.Add(new Transaction
            {
                BankAccount = account,
                Type = TransactionType.Deposit,
                Amount = request.InitialDeposit,
                BalanceAfter = account.Balance,
                Description = "Initial deposit",
                ReferenceNumber = ReferenceNumber.New(),
                PerformedBy = performedBy
            });
        }

        // A single SaveChanges call is already atomic, so no explicit transaction is needed here.
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Opened account {AccountNumber} for customer {CustomerId}", account.AccountNumber, customer.Id);
        return account.ToResponse();
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken ct)
    {
        while (true)
        {
            // 10 digits, never starting with 0.
            var number = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
            if (!await _accounts.AccountNumberExistsAsync(number, ct))
            {
                return number;
            }
        }
    }
}
