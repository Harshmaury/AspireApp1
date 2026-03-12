// UMS — University Management System
// Key: UMS-SHARED-P0-002
// Layer: Shared / Domain
// ─────────────────────────────────────────────────────────────
// Canonical aggregate root base for ALL 9 services.
// Replaces the 7 per-service AggregateRoot copies in Domain/Common/.
//
// Per-service copies to DELETE after this ships:
//   Academic.Domain/Common/AggregateRoot.cs
//   Attendance.Domain/Common/AggregateRoot.cs
//   Examination.Domain/Common/AggregateRoot.cs
//   Faculty.Domain/Common/AggregateRoot.cs
//   Fee.Domain/Common/AggregateRoot.cs
//   Hostel.Domain/Common/AggregateRoot.cs
//   Notification.Domain/Common/AggregateRoot.cs
// ─────────────────────────────────────────────────────────────
namespace UMS.SharedKernel.Domain;

/// <summary>
/// Abstract base for all DDD aggregate roots in UMS.
/// Inherits <see cref="BaseEntity"/> (Id + TenantId + timestamps)
/// and implements <see cref="IAggregateRoot"/> (domain event collection).
/// </summary>
public abstract class AggregateRoot : BaseEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc/>
    public IReadOnlyList<MediatR.INotification> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc/>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Raises a domain event. Call this inside mutating methods on the aggregate.
    /// The event is collected in memory and dispatched after SaveChanges by
    /// <see cref="UMS.SharedKernel.Infrastructure.DomainEventDispatcherInterceptorBase"/>.
    /// </summary>
    /// <example>
    /// protected void Enroll(Guid courseId)
    /// {
    ///     // ... state change ...
    ///     RaiseDomainEvent(new StudentEnrolledEvent(Id, courseId, TenantId));
    /// }
    /// </example>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
}
