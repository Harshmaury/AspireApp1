using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Programme.Queries;
public sealed record GetProgrammeByIdQuery(Guid Id, Guid TenantId) : IRequest<ProgrammeDto?>;
public sealed class GetProgrammeByIdQueryHandler : IRequestHandler<GetProgrammeByIdQuery, ProgrammeDto?>
{
    private readonly IProgrammeRepository _repo;
    public GetProgrammeByIdQueryHandler(IProgrammeRepository repo) => _repo = repo;
    public async Task<ProgrammeDto?> Handle(GetProgrammeByIdQuery req, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(req.Id, req.TenantId, ct);
        if (p is null) return null;
        return new ProgrammeDto(p.Id, p.TenantId, p.DepartmentId, p.Name, p.Code, p.Degree, p.DurationYears, p.TotalCredits, p.IntakeCapacity, p.Status.ToString(), p.CreatedAt);
    }
}