using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Curriculum.Queries;
public sealed record GetCurriculumByProgrammeQuery(Guid ProgrammeId, Guid TenantId, string Version) : IRequest<IReadOnlyList<CurriculumDto>>;
public sealed class GetCurriculumByProgrammeQueryHandler : IRequestHandler<GetCurriculumByProgrammeQuery, IReadOnlyList<CurriculumDto>>
{
    private readonly ICurriculumRepository _repo;
    public GetCurriculumByProgrammeQueryHandler(ICurriculumRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<CurriculumDto>> Handle(GetCurriculumByProgrammeQuery req, CancellationToken ct)
    {
        var items = await _repo.GetByProgrammeAsync(req.ProgrammeId, req.TenantId, req.Version, ct);
        return items.Select(c => new CurriculumDto(c.Id, c.ProgrammeId, c.CourseId, c.Semester, c.IsElective, c.IsOptional, c.Version)).ToList();
    }
}