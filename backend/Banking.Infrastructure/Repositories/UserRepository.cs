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
}
