using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditLog entry, CancellationToken ct = default);
}
