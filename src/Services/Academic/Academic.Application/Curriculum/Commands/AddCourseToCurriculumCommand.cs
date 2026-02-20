using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Curriculum.Commands;
public sealed record AddCourseToCurriculumCommand(Guid TenantId, Guid ProgrammeId, Guid CourseId, int Semester, bool IsElective, bool IsOptional, string Version) : IRequest<CurriculumDto>;