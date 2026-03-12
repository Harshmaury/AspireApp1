// UMS â€” University Management System
// Key:     UMS-SHARED-P0-002
// Service: SharedKernel
// Layer:   Domain
namespace UMS.SharedKernel.Domain;

public abstract class BaseEntity
{
    public Guid           Id         { get; protected set; } = Guid.NewGuid();
    public Guid           TenantId   { get; protected set; }
    public DateTimeOffset CreatedAt  { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt  { get; protected set; } = DateTimeOffset.UtcNow;
    public uint           RowVersion { get; private set; }

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
