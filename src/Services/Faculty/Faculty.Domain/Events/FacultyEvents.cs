using MediatR;
using UMS.SharedKernel.Kafka;

namespace Faculty.Domain.Events;

public sealed record FacultyCreatedEvent(Guid FacultyId, Guid TenantId, Guid UserId) : INotification, ITenantedEvent;
public sealed record FacultyStatusChangedEvent(Guid FacultyId, Guid TenantId, string NewStatus) : INotification, ITenantedEvent;
public sealed record CourseAssignedEvent(Guid FacultyId, Guid TenantId, Guid CourseId, string AcademicYear) : INotification, ITenantedEvent;
public sealed record PublicationAddedEvent(Guid FacultyId, Guid TenantId, Guid PublicationId, string Type) : INotification, ITenantedEvent;