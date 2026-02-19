using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _db;

    public TenantRepository(ApplicationDbContext db) => _db = db;

    public async Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default)
        => await _db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive, ct);

    public async Task<bool> ExistsAsync(string slug, CancellationToken ct = default)
        => await _db.Tenants
            .AnyAsync(t => t.Slug == slug.ToLowerInvariant(), ct);
}
