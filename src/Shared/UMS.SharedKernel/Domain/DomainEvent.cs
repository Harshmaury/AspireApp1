// UMS — University Management System
// Key: UMS-SHARED-P0-002
// Layer: Shared / Domain
// ─────────────────────────────────────────────────────────────
// Abstract base record for ALL domain events in ALL 9 services.
// Use a record (not class) — events are immutable value objects.
// ─────────────────────────────────────────────────────────────
namespace UMS.SharedKernel.Domain;

/// <summary>
/// Abstract base record for all UMS domain events.
/// Inherit in every service's Domain/Events/ folder:
/// <code>
///   public sealed record StudentCreatedEvent(Guid StudentId, Guid TenantId)
///       : DomainEvent(TenantId);
/// </code>
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public Guid TenantId { get; }

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Base constructor — always pass the owning tenant's ID.
    /// </summary>
    protected DomainEvent(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
