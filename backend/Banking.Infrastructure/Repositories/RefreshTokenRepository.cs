using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly BankingDbContext _db;

    public RefreshTokenRepository(BankingDbContext db)
    {
        _db = db;
    }

    // Tracked, because the caller revokes the token and saves.
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<RefreshToken>> GetUnrevokedForUserAsync(int userId, CancellationToken ct = default) =>
        _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);

    public void Add(RefreshToken token) => _db.RefreshTokens.Add(token);
}
