using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.AcademicCalendar.Commands;
public sealed record ActivateAcademicCalendarCommand(Guid Id, Guid TenantId) : IRequest;
public sealed class ActivateAcademicCalendarCommandHandler : IRequestHandler<ActivateAcademicCalendarCommand>
{
    private readonly IAcademicCalendarRepository _repo;
    public ActivateAcademicCalendarCommandHandler(IAcademicCalendarRepository repo) => _repo = repo;
    public async Task Handle(ActivateAcademicCalendarCommand req, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(req.Id, req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Academic calendar {req.Id} not found.");
        a.Activate();
        await _repo.UpdateAsync(a, ct);
    }
}