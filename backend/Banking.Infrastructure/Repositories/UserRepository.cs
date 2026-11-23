using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BankingDbContext _db;

    public UserRepository(BankingDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<(List<User> Items, int TotalCount)> GetPageAsync(int skip, int take, CancellationToken ct = default)
    {
        var total = await _db.Users.CountAsync(ct);
        var items = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Username == username, ct);

    public void Add(User user) => _db.Users.Add(user);
}
