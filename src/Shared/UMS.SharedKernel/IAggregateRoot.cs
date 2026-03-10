// ============================================================
// UMS.SharedKernel — IAggregateRoot
// All domain aggregate roots across all services implement this.
// ============================================================
using MediatR;

namespace UMS.SharedKernel;

/// <summary>
/// Marker interface for all DDD aggregate roots in UMS.
/// Provides access to in-memory domain events raised during state changes.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Domain events raised during this unit of work (not yet dispatched).</summary>
    IReadOnlyList<INotification> DomainEvents { get; }

    /// <summary>Clears the in-memory event list after dispatch.</summary>
    void ClearDomainEvents();
}
