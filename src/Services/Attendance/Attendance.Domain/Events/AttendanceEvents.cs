using MediatR;
using UMS.SharedKernel.Kafka;

namespace Attendance.Domain.Events;

public sealed record AttendanceShortageFlaggedEvent(Guid StudentId, Guid TenantId, Guid CourseId, decimal Percentage) : INotification, ITenantedEvent;
public sealed record CondonationApprovedEvent(Guid CondonationId, Guid StudentId, Guid TenantId, Guid CourseId) : INotification, ITenantedEvent;
public sealed record AttendanceMarkedEvent(Guid StudentId, Guid TenantId, Guid CourseId, DateOnly Date, bool IsPresent) : INotification, ITenantedEvent;