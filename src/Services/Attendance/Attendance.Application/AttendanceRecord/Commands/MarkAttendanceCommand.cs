using MediatR;
using Attendance.Domain.Enums;
namespace Attendance.Application.AttendanceRecord.Commands;
public sealed record MarkAttendanceCommand(
    Guid TenantId,
    Guid StudentId,
    Guid CourseId,
    string AcademicYear,
    int Semester,
    DateOnly Date,
    string ClassType,
    bool IsPresent,
    Guid MarkedBy) : IRequest<Guid>;
