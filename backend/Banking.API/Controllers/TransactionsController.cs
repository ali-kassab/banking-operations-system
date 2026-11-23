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

    /// <summary>Transactions across all accounts, newest first, one page at a time.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAll([FromQuery] PageQuery paging, CancellationToken ct) =>
        Ok(await _transactionService.GetAllAsync(paging, ct));
}
