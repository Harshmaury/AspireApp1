using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Programme.Commands;
public sealed record UpdateProgrammeCommand(Guid Id, Guid TenantId, string Name, int DurationYears, int TotalCredits, int IntakeCapacity) : IRequest;
public sealed class UpdateProgrammeCommandHandler : IRequestHandler<UpdateProgrammeCommand>
{
    private readonly IProgrammeRepository _repo;
    public UpdateProgrammeCommandHandler(IProgrammeRepository repo) => _repo = repo;
    public async Task Handle(UpdateProgrammeCommand req, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(req.Id, req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Programme {req.Id} not found.");
        p.Update(req.Name, req.DurationYears, req.TotalCredits, req.IntakeCapacity);
        await _repo.UpdateAsync(p, ct);
    }
}