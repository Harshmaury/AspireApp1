using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Programme.Queries;
public sealed class GetProgrammesByDepartmentQueryHandler : IRequestHandler<GetProgrammesByDepartmentQuery, IReadOnlyList<ProgrammeDto>>
{
    private readonly IProgrammeRepository _repo;
    public GetProgrammesByDepartmentQueryHandler(IProgrammeRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<ProgrammeDto>> Handle(GetProgrammesByDepartmentQuery query, CancellationToken ct)
    {
        var progs = await _repo.GetByDepartmentAsync(query.DepartmentId, query.TenantId, ct);
        return progs.Select(p => new ProgrammeDto(p.Id, p.TenantId, p.DepartmentId, p.Name, p.Code, p.Degree, p.DurationYears, p.TotalCredits, p.IntakeCapacity, p.Status.ToString(), p.CreatedAt)).ToList();
    }
}