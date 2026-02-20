using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Curriculum.Commands;
public sealed class AddCourseToCurriculumCommandHandler : IRequestHandler<AddCourseToCurriculumCommand, CurriculumDto>
{
    private readonly ICurriculumRepository _repo;
    public AddCourseToCurriculumCommandHandler(ICurriculumRepository repo) => _repo = repo;
    public async Task<CurriculumDto> Handle(AddCourseToCurriculumCommand cmd, CancellationToken ct)
    {
        var curriculum = Academic.Domain.Entities.Curriculum.Create(cmd.TenantId, cmd.ProgrammeId, cmd.CourseId, cmd.Semester, cmd.IsElective, cmd.IsOptional, cmd.Version);
        await _repo.AddAsync(curriculum, ct);
        return new CurriculumDto(curriculum.Id, curriculum.ProgrammeId, curriculum.CourseId, curriculum.Semester, curriculum.IsElective, curriculum.IsOptional, curriculum.Version);
    }
}