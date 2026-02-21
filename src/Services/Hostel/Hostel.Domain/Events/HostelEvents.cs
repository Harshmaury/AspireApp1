using MediatR;
namespace Hostel.Domain.Events;
public sealed record RoomAllottedEvent(Guid AllotmentId, Guid StudentId, Guid TenantId, Guid RoomId) : INotification;
public sealed record RoomVacatedEvent(Guid AllotmentId, Guid StudentId, Guid TenantId, Guid RoomId) : INotification;
public sealed record ComplaintRaisedEvent(Guid ComplaintId, Guid StudentId, Guid TenantId, string Category) : INotification;
public sealed record ComplaintResolvedEvent(Guid ComplaintId, Guid TenantId) : INotification;
