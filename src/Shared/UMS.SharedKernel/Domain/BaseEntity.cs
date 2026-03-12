// UMS — University Management System
// Key: UMS-SHARED-P0-002
// Layer: Shared / Domain
// ─────────────────────────────────────────────────────────────
// Canonical base for EVERY entity in ALL 9 services.
// Rules enforced by Aegis TenantIsolationRule:
//   • Every entity MUST inherit BaseEntity
//   • TenantId MUST be set at construction — never default(Guid)
//   • Do NOT redeclare Id or TenantId in subclasses
// ─────────────────────────────────────────────────────────────
namespace UMS.SharedKernel.Domain;

/// <summary>
/// Canonical base entity for all UMS domain entities across all services.
/// Provides <see cref="Id"/> (Guid PK) and <see cref="TenantId"/> for
/// row-level multi-tenancy enforced by EF Core global query filters.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key — set once at construction, never reassigned.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant this entity belongs to.
    /// MUST be set at construction via the protected setter.
    /// EF Core global query filters use this for row-level isolation.
    /// </summary>
    public Guid TenantId { get; protected set; }

    /// <summary>
    /// UTC timestamp when the entity was first persisted.
    /// Set by EF Core value generation or the constructor — not updated on change.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp of the last state change.
    /// Update this in every mutating method on the entity.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Concurrency token — EF Core uses this for optimistic concurrency.
    /// Incremented automatically by EF Core on every SaveChanges.
    /// </summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Call at the start of every mutating method to keep UpdatedAt current.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
