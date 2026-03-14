// UMS — University Management System
// Key:     UMS-IDENTITY-P2-012
// Service: Identity
// Layer:   Infrastructure / Persistence / Repositories
namespace Identity.Infrastructure.Persistence.Repositories;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _db;

    public TenantRepository(ApplicationDbContext db) => _db = db;

    public async Task<Tenant?> FindByIdAsync(
        Guid tenantId, CancellationToken ct = default)
        => await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    public async Task<Tenant?> FindBySlugAsync(
        string slug, CancellationToken ct = default)
        => await _db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public async Task<bool> ExistsAsync(
        string slug, CancellationToken ct = default)
        => await _db.Tenants
            .AnyAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await _db.Tenants.AddAsync(tenant, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
    }
}
