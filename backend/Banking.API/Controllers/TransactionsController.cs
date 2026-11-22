using Banking.Application.Dtos;
using Banking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api")]
[Authorize] // Admin and Employee
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionsController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<TransferResponse>> Transfer(TransferRequest request, CancellationToken ct) =>
        Ok(await _transactionService.TransferAsync(request, User.Identity!.Name!, ct));

    /// <summary>Most recent transactions across all accounts.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionResponse>>> GetRecent([FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _transactionService.GetRecentAsync(take, ct));
}
