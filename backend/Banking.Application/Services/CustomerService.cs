using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

public class CustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customers, IUnitOfWork unitOfWork, ILogger<CustomerService> logger)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResponse<CustomerResponse>> GetPageAsync(string? search, PageQuery page, CancellationToken ct = default)
    {
        var (customers, total) = await _customers.GetPageAsync(search, page.Skip, page.PageSize, ct);
        return new PagedResponse<CustomerResponse>(
            customers.Select(c => c.ToResponse()).ToList(), page.Page, page.PageSize, total);
    }

    public async Task<CustomerResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Customer {id} was not found.");

        return customer.ToResponse();
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var nationalId = request.NationalId.Trim();

        if (await _customers.EmailExistsAsync(email, ct))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        if (await _customers.NationalIdExistsAsync(nationalId, ct))
        {
            throw new ConflictException($"A customer with national ID '{nationalId}' already exists.");
        }

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            NationalId = nationalId
        };

        _customers.Add(customer);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created customer {CustomerId}", customer.Id);
        return customer.ToResponse();
    }
}
