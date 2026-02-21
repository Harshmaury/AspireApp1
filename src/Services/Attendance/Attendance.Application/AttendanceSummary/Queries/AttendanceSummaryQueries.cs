using MediatR;
using Attendance.Application.DTOs;
namespace Attendance.Application.AttendanceSummary.Queries;
public sealed record GetStudentSummaryQuery(Guid StudentId, Guid TenantId) : IRequest<List<AttendanceSummaryDto>>;
public sealed record GetStudentCourseSummaryQuery(Guid StudentId, Guid CourseId, Guid TenantId) : IRequest<AttendanceSummaryDto?>;
public sealed record GetShortageListQuery(Guid TenantId) : IRequest<List<AttendanceSummaryDto>>;
