using MediatR;
using UMS.SharedKernel.Kafka;

namespace Hostel.Domain.Events;

public sealed record RoomAllottedEvent(Guid AllotmentId, Guid StudentId, Guid TenantId, Guid RoomId) : INotification, ITenantedEvent;
public sealed record RoomVacatedEvent(Guid AllotmentId, Guid StudentId, Guid TenantId, Guid RoomId) : INotification, ITenantedEvent;
public sealed record ComplaintRaisedEvent(Guid ComplaintId, Guid StudentId, Guid TenantId, string Category) : INotification, ITenantedEvent;
public sealed record ComplaintResolvedEvent(Guid ComplaintId, Guid TenantId) : INotification, ITenantedEvent;