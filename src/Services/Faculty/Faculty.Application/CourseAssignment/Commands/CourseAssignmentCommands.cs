using MediatR;
namespace Faculty.Application.CourseAssignment.Commands;
public sealed record AssignCourseCommand(Guid TenantId, Guid FacultyId, Guid CourseId, string AcademicYear, int Semester, string? Section = null) : IRequest<Guid>;
public sealed record UnassignCourseCommand(Guid TenantId, Guid AssignmentId) : IRequest;
