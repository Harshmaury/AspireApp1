using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Course.Queries;
public sealed record GetCoursesByDepartmentQuery(Guid DepartmentId, Guid TenantId) : IRequest<IReadOnlyList<CourseDto>>;