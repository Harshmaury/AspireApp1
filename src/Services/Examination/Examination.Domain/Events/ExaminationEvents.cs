using MediatR;
using UMS.SharedKernel.Kafka;

namespace Examination.Domain.Events;

public sealed record MarksPublishedEvent(Guid ExamScheduleId, Guid TenantId) : INotification, ITenantedEvent;
public sealed record ResultDeclaredEvent(Guid StudentId, Guid TenantId, string AcademicYear, int Semester, decimal SGPA, decimal CGPA) : INotification, ITenantedEvent;
public sealed record StudentBacklogEvent(Guid StudentId, Guid TenantId, Guid CourseId) : INotification, ITenantedEvent;
public sealed record StudentClearedBacklogEvent(Guid StudentId, Guid TenantId, Guid CourseId) : INotification, ITenantedEvent;
public sealed record ExamScheduledEvent(Guid ExamScheduleId, Guid TenantId, Guid CourseId) : INotification, ITenantedEvent;