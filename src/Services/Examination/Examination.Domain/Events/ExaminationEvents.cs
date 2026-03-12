using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace Examination.Domain.Events;

public sealed record MarksPublishedEvent(Guid ExamScheduleId, Guid TenantId) : IDomainEvent, ITenantedEvent;
public sealed record ResultDeclaredEvent(Guid StudentId, Guid TenantId, string AcademicYear, int Semester, decimal SGPA, decimal CGPA) : IDomainEvent, ITenantedEvent;
public sealed record StudentBacklogEvent(Guid StudentId, Guid TenantId, Guid CourseId) : IDomainEvent, ITenantedEvent;
public sealed record StudentClearedBacklogEvent(Guid StudentId, Guid TenantId, Guid CourseId) : IDomainEvent, ITenantedEvent;
public sealed record ExamScheduledEvent(Guid ExamScheduleId, Guid TenantId, Guid CourseId) : IDomainEvent, ITenantedEvent;
