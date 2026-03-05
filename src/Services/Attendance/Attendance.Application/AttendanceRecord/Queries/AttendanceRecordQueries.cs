using Attendance.Application.DTOs;
using MediatR;
namespace Attendance.Application.AttendanceRecord.Queries;
public sealed record GetStudentAttendanceQuery(Guid StudentId, Guid CourseId, Guid TenantId) : IRequest<List<AttendanceRecordDto>>;
public sealed record GetCourseAttendanceByDateQuery(Guid CourseId, DateOnly Date, Guid TenantId) : IRequest<List<AttendanceRecordDto>>;
