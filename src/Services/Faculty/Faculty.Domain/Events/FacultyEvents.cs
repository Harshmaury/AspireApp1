using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace Faculty.Domain.Events;

public sealed record FacultyCreatedEvent(Guid FacultyId, Guid TenantId, Guid UserId) : IDomainEvent, ITenantedEvent;
public sealed record FacultyStatusChangedEvent(Guid FacultyId, Guid TenantId, string NewStatus) : IDomainEvent, ITenantedEvent;
public sealed record CourseAssignedEvent(Guid FacultyId, Guid TenantId, Guid CourseId, string AcademicYear) : IDomainEvent, ITenantedEvent;
public sealed record PublicationAddedEvent(Guid FacultyId, Guid TenantId, Guid PublicationId, string Type) : IDomainEvent, ITenantedEvent;
