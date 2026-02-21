using MediatR;
namespace Attendance.Domain.Events;
public sealed record AttendanceShortageFlaggedEvent(Guid StudentId, Guid TenantId, Guid CourseId, decimal Percentage) : INotification;
public sealed record CondonationApprovedEvent(Guid CondonationId, Guid StudentId, Guid TenantId, Guid CourseId) : INotification;
public sealed record AttendanceMarkedEvent(Guid StudentId, Guid TenantId, Guid CourseId, DateOnly Date, bool IsPresent) : INotification;
