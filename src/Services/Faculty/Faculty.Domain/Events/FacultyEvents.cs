using MediatR;
namespace Faculty.Domain.Events;
public sealed record FacultyCreatedEvent(Guid FacultyId, Guid TenantId, Guid UserId) : INotification;
public sealed record FacultyStatusChangedEvent(Guid FacultyId, Guid TenantId, string NewStatus) : INotification;
public sealed record CourseAssignedEvent(Guid FacultyId, Guid TenantId, Guid CourseId, string AcademicYear) : INotification;
public sealed record PublicationAddedEvent(Guid FacultyId, Guid TenantId, Guid PublicationId, string Type) : INotification;
