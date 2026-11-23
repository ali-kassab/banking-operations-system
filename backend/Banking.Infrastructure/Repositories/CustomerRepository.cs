using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly BankingDbContext _db;

    public CustomerRepository(BankingDbContext db)
    {
        _db = db;
    }

    public async Task<(List<Customer> Items, int TotalCount)> GetPageAsync(string? search, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Email.Contains(term) ||
                c.NationalId.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(c => c.Accounts) // needed for AccountCount in the response
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id) // stable order for paging
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Customers.Include(c => c.Accounts).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        _db.Customers.AnyAsync(c => c.Email == email, ct);

    public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct = default) =>
        _db.Customers.AnyAsync(c => c.NationalId == nationalId, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _db.Customers.CountAsync(ct);

    public void Add(Customer customer) => _db.Customers.Add(customer);
}
