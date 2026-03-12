// UMS — University Management System
// Key: UMS-SHARED-P0-002
// Layer: Shared / Domain
// ─────────────────────────────────────────────────────────────
// All domain events in all 9 services implement this interface.
// Extends MediatR INotification so events route through the
// MediatR publisher without additional casting.
// ─────────────────────────────────────────────────────────────
using MediatR;

namespace UMS.SharedKernel.Domain;

/// <summary>
/// Marker interface for all UMS domain events.
/// Implement on every event raised by an aggregate root.
/// Events are dispatched in-process by <see cref="UMS.SharedKernel.Infrastructure.DomainEventDispatcherInterceptorBase"/>
/// and written to the outbox table for Kafka relay.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Unique event identifier — generated at raise time.</summary>
    Guid EventId { get; }

    /// <summary>Tenant that owns the aggregate that raised this event.</summary>
    Guid TenantId { get; }

    /// <summary>UTC time the event was raised.</summary>
    DateTimeOffset OccurredAt { get; }
}
