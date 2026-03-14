using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class VerificationTokenRepository : IVerificationTokenRepository
{
    private readonly ApplicationDbContext _db;
    public VerificationTokenRepository(ApplicationDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task CreateAsync(VerificationToken token, CancellationToken ct = default)
    {
        await _db.VerificationTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<VerificationToken?> FindByHashAsync(string tokenHash, TokenPurpose purpose, CancellationToken ct = default)
        => await _db.VerificationTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && t.Purpose == purpose &&
                 t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, ct);

    public async Task UpdateAsync(VerificationToken token, CancellationToken ct = default)
    {
        if (_db.Entry(token).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached VerificationToken (Id={token.Id}). " +
                "Fetch the token via FindByHashAsync in the same handler scope before mutating it.");
        await _db.SaveChangesAsync(ct);
    }

    public Task InvalidateByUserAsync(Guid userId, TokenPurpose purpose, CancellationToken ct = default)
        => InvalidateAllForUserAsync(userId, purpose, ct);

    public async Task InvalidateAllForUserAsync(Guid userId, TokenPurpose purpose, CancellationToken ct = default)
    {
        var tokens = await _db.VerificationTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.MarkUsed();

        if (tokens.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}

