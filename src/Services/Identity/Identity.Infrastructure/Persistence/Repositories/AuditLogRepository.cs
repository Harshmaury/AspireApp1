// ═══════════════════════════════════════════════════════════════════════
// FILE 5 (NEW): Identity.Infrastructure/Persistence/Repositories/AuditLogRepository.cs
// ═══════════════════════════════════════════════════════════════════════
namespace Identity.Infrastructure.Persistence.Repositories;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;

internal sealed class AuditLogRepository : IAuditLogger
{
    private readonly ApplicationDbContext _db;

    public AuditLogRepository(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(AuditLog entry, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}