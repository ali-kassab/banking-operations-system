using Banking.API.Authorization;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize] // Admin and Employee, unless an action says otherwise
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly TransactionService _transactionService;

    public AccountsController(AccountService accountService, TransactionService transactionService)
    {
        _accountService = accountService;
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountResponse>>> GetAll([FromQuery] int? customerId, CancellationToken ct) =>
        Ok(await _accountService.GetAllAsync(customerId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await _accountService.GetByIdAsync(id, ct));

    /// <summary>Opening an account is restricted to administrators.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var account = await _accountService.CreateAsync(request, User.Identity!.Name!, ct);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPost("{id:int}/deposit")]
    public async Task<ActionResult<TransactionResponse>> Deposit(int id, AmountRequest request, CancellationToken ct) =>
        Ok(await _transactionService.DepositAsync(id, request, User.Identity!.Name!, ct));

    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<TransactionResponse>> Withdraw(int id, AmountRequest request, CancellationToken ct) =>
        Ok(await _transactionService.WithdrawAsync(id, request, User.Identity!.Name!, ct));

    [HttpGet("{id:int}/transactions")]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions(int id, CancellationToken ct) =>
        Ok(await _transactionService.GetAccountHistoryAsync(id, ct));
}
