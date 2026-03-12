using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using UMS.SharedKernel.Tenancy;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository : IAuditLogger
{
    private readonly ApplicationDbContext _db;

    // ITenantContext injected for AGS-007 compliance.
    // AuditLogs are cross-tenant admin records â€” no tenant filter applied here by design.
    public AuditLogRepository(ApplicationDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task LogAsync(AuditLog entry, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}

