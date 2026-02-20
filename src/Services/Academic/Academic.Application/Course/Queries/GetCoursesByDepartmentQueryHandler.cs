using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Course.Queries;
public sealed class GetCoursesByDepartmentQueryHandler : IRequestHandler<GetCoursesByDepartmentQuery, IReadOnlyList<CourseDto>>
{
    private readonly ICourseRepository _repo;
    public GetCoursesByDepartmentQueryHandler(ICourseRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesByDepartmentQuery query, CancellationToken ct)
    {
        var courses = await _repo.GetByDepartmentAsync(query.DepartmentId, query.TenantId, ct);
        return courses.Select(c => new CourseDto(c.Id, c.TenantId, c.DepartmentId, c.Name, c.Code, c.Description, c.Credits, c.LectureHours, c.TutorialHours, c.PracticalHours, c.CourseType, c.MaxEnrollment, c.Status.ToString(), c.CreatedAt)).ToList();
    }
}