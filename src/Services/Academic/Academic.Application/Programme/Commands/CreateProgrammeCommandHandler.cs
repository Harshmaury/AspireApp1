using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using Academic.Domain.Exceptions;
using MediatR;
namespace Academic.Application.Programme.Commands;
public sealed class CreateProgrammeCommandHandler : IRequestHandler<CreateProgrammeCommand, ProgrammeDto>
{
    private readonly IProgrammeRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    public CreateProgrammeCommandHandler(IProgrammeRepository repo, IDepartmentRepository deptRepo) { _repo = repo; _deptRepo = deptRepo; }
    public async Task<ProgrammeDto> Handle(CreateProgrammeCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsAsync(cmd.Code, cmd.TenantId, ct))
            throw new AcademicDomainException("DUPLICATE_CODE", $"Programme with code '{cmd.Code}' already exists.");
        var dept = await _deptRepo.GetByIdAsync(cmd.DepartmentId, cmd.TenantId, ct)
            ?? throw new AcademicDomainException("DEPT_NOT_FOUND", $"Department '{cmd.DepartmentId}' not found.");
        var prog = Academic.Domain.Entities.Programme.Create(cmd.TenantId, cmd.DepartmentId, cmd.Name, cmd.Code, cmd.Degree, cmd.DurationYears, cmd.TotalCredits, cmd.IntakeCapacity);
        await _repo.AddAsync(prog, ct);
        return new ProgrammeDto(prog.Id, prog.TenantId, prog.DepartmentId, prog.Name, prog.Code, prog.Degree, prog.DurationYears, prog.TotalCredits, prog.IntakeCapacity, prog.Status.ToString(), prog.CreatedAt);
    }
}