using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <summary>
/// All operations that move money: deposits, withdrawals and transfers,
/// plus reading the transaction history.
/// </summary>
public class TransactionService
{
    private readonly IBankAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IBankAccountRepository accounts,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        ILogger<TransactionService> logger)
    {
        _accounts = accounts;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> DepositAsync(int accountId, AmountRequest request, string performedBy, CancellationToken ct = default)
    {
        var account = await GetAccountOrThrowAsync(accountId, ct);

        account.Deposit(request.Amount); // throws DomainException for invalid amounts

        var transaction = Record(account, TransactionType.Deposit, request.Amount,
            request.Description ?? "Cash deposit", ReferenceNumber.New(), null, performedBy);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deposited {Amount} into account {AccountId}", request.Amount, accountId);
        return transaction.ToResponse();
    }

    public async Task<TransactionResponse> WithdrawAsync(int accountId, AmountRequest request, string performedBy, CancellationToken ct = default)
    {
        var account = await GetAccountOrThrowAsync(accountId, ct);

        account.Withdraw(request.Amount); // throws DomainException for invalid amounts or insufficient funds

        var transaction = Record(account, TransactionType.Withdrawal, request.Amount,
            request.Description ?? "Cash withdrawal", ReferenceNumber.New(), null, performedBy);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Withdrew {Amount} from account {AccountId}", request.Amount, accountId);
        return transaction.ToResponse();
    }

    public async Task<TransferResponse> TransferAsync(TransferRequest request, string performedBy, CancellationToken ct = default)
    {
        if (request.FromAccountId == request.ToAccountId)
        {
            throw new DomainException("Source and destination accounts must be different.");
        }

        BankAccount.ValidateAmount(request.Amount);

        var from = await GetAccountOrThrowAsync(request.FromAccountId, ct);
        var to = await GetAccountOrThrowAsync(request.ToAccountId, ct);
        var reference = ReferenceNumber.New();
        var description = request.Description ?? $"Transfer {from.AccountNumber} → {to.AccountNumber}";

        // Both balance changes and both history rows are written in ONE database transaction:
        // either everything is saved, or (on any error) nothing is.
        await _unitOfWork.ExecuteInTransactionAsync(() =>
        {
            from.Withdraw(request.Amount); // throws if the balance is too low
            to.Deposit(request.Amount);

            Record(from, TransactionType.TransferOut, request.Amount, description, reference, to.Id, performedBy);
            Record(to, TransactionType.TransferIn, request.Amount, description, reference, from.Id, performedBy);

            return Task.CompletedTask;
        }, ct);

        _logger.LogInformation("Transferred {Amount} from account {FromId} to {ToId} (ref {Reference})",
            request.Amount, from.Id, to.Id, reference);

        return new TransferResponse(reference, request.Amount, from.ToResponse(), to.ToResponse());
    }

    public async Task<List<TransactionResponse>> GetAccountHistoryAsync(int accountId, CancellationToken ct = default)
    {
        await GetAccountOrThrowAsync(accountId, ct);
        var transactions = await _transactions.GetByAccountIdAsync(accountId, ct);
        return transactions.Select(t => t.ToResponse()).ToList();
    }

    public async Task<List<TransactionResponse>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 200);
        var transactions = await _transactions.GetRecentAsync(count, ct);
        return transactions.Select(t => t.ToResponse()).ToList();
    }

    private async Task<BankAccount> GetAccountOrThrowAsync(int accountId, CancellationToken ct) =>
        await _accounts.GetByIdAsync(accountId, ct)
        ?? throw new NotFoundException($"Account {accountId} was not found.");

    private Transaction Record(BankAccount account, TransactionType type, decimal amount,
        string description, string reference, int? counterpartyAccountId, string performedBy)
    {
        var transaction = new Transaction
        {
            BankAccount = account,
            Type = type,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = description,
            ReferenceNumber = reference,
            CounterpartyAccountId = counterpartyAccountId,
            PerformedBy = performedBy
        };

        _transactions.Add(transaction);
        return transaction;
    }
}
