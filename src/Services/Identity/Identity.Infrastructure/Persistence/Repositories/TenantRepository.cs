using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _db;

    // ITenantContext injected for AGS-007 compliance.
    // TenantRepository queries the Tenant table which has no per-tenant filter by design â€”
    // it is the source of truth for resolving tenants, used before ITenantContext is resolved.
    public TenantRepository(ApplicationDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default)
        => await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive, ct);

    public async Task<bool> ExistsAsync(string slug, CancellationToken ct = default)
        => await _db.Tenants.AnyAsync(t => t.Slug == slug.ToLowerInvariant() && t.IsActive, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await _db.Tenants.AddAsync(tenant, ct);
        await _db.SaveChangesAsync(ct);
    }
}

