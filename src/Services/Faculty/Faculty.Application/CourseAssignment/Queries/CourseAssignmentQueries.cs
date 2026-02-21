using MediatR;
using Faculty.Application.DTOs;
namespace Faculty.Application.CourseAssignment.Queries;
public sealed record GetFacultyCourseAssignmentsQuery(Guid FacultyId, Guid TenantId) : IRequest<List<CourseAssignmentDto>>;
public sealed record GetFacultyCoursesByYearQuery(Guid FacultyId, string AcademicYear, Guid TenantId) : IRequest<List<CourseAssignmentDto>>;
