using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using Academic.Domain.Exceptions;
using MediatR;
namespace Academic.Application.Course.Commands;
public sealed class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
{
    private readonly ICourseRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    public CreateCourseCommandHandler(ICourseRepository repo, IDepartmentRepository deptRepo) { _repo = repo; _deptRepo = deptRepo; }
    public async Task<CourseDto> Handle(CreateCourseCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.Code, cmd.TenantId, ct))
            throw new AcademicDomainException("DUPLICATE_CODE", $"Course with code '{cmd.Code}' already exists.");
        var dept = await _deptRepo.GetByIdAsync(cmd.DepartmentId, cmd.TenantId, ct)
            ?? throw new AcademicDomainException("DEPT_NOT_FOUND", $"Department '{cmd.DepartmentId}' not found.");
        var course = Academic.Domain.Entities.Course.Create(cmd.TenantId, cmd.DepartmentId, cmd.Name, cmd.Code, cmd.Credits, cmd.CourseType, cmd.LectureHours, cmd.TutorialHours, cmd.PracticalHours, cmd.MaxEnrollment, cmd.Description);
        await _repo.AddAsync(course, ct);
        return new CourseDto(course.Id, course.TenantId, course.DepartmentId, course.Name, course.Code, course.Description, course.Credits, course.LectureHours, course.TutorialHours, course.PracticalHours, course.CourseType, course.MaxEnrollment, course.Status.ToString(), course.CreatedAt);
    }
}