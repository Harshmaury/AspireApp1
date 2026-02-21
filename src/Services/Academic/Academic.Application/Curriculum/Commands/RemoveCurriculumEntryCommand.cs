using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Curriculum.Commands;
public sealed record RemoveCurriculumEntryCommand(Guid Id, Guid TenantId) : IRequest;
public sealed class RemoveCurriculumEntryCommandHandler : IRequestHandler<RemoveCurriculumEntryCommand>
{
    private readonly ICurriculumRepository _repo;
    public RemoveCurriculumEntryCommandHandler(ICurriculumRepository repo) => _repo = repo;
    public async Task Handle(RemoveCurriculumEntryCommand req, CancellationToken ct)
        => await _repo.DeleteAsync(req.Id, req.TenantId, ct);
}