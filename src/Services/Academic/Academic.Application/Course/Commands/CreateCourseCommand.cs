using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Course.Commands;
public sealed record CreateCourseCommand(Guid TenantId, Guid DepartmentId, string Name, string Code, int Credits, string CourseType, int LectureHours, int TutorialHours, int PracticalHours, int MaxEnrollment, string? Description) : IRequest<CourseDto>;