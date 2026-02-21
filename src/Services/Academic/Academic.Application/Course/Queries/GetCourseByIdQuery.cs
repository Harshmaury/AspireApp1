using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Course.Queries;
public sealed record GetCourseByIdQuery(Guid Id, Guid TenantId) : IRequest<CourseDto?>;
public sealed class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDto?>
{
    private readonly ICourseRepository _repo;
    public GetCourseByIdQueryHandler(ICourseRepository repo) => _repo = repo;
    public async Task<CourseDto?> Handle(GetCourseByIdQuery req, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(req.Id, req.TenantId, ct);
        if (c is null) return null;
        return new CourseDto(c.Id, c.TenantId, c.DepartmentId, c.Name, c.Code, c.Description, c.Credits, c.LectureHours, c.TutorialHours, c.PracticalHours, c.CourseType, c.MaxEnrollment, c.Status.ToString(), c.CreatedAt);
    }
}