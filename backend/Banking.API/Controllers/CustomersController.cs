using Banking.Application.Dtos;
using Banking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize] // Admin and Employee
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetAll(
        [FromQuery] string? search, [FromQuery] PageQuery paging, CancellationToken ct) =>
        Ok(await _customerService.GetPageAsync(search, paging, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await _customerService.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = await _customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }
}
