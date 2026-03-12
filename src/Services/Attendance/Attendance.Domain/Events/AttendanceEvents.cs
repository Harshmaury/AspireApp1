using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace Attendance.Domain.Events;

public sealed record AttendanceShortageFlaggedEvent(Guid StudentId, Guid TenantId, Guid CourseId, decimal Percentage) : IDomainEvent, ITenantedEvent;
public sealed record CondonationApprovedEvent(Guid CondonationId, Guid StudentId, Guid TenantId, Guid CourseId) : IDomainEvent, ITenantedEvent;
public sealed record AttendanceMarkedEvent(Guid StudentId, Guid TenantId, Guid CourseId, DateOnly Date, bool IsPresent) : IDomainEvent, ITenantedEvent;
