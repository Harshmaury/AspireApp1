using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Course.Commands;
public sealed record PublishCourseCommand(Guid Id, Guid TenantId) : IRequest;
public sealed class PublishCourseCommandHandler : IRequestHandler<PublishCourseCommand>
{
    private readonly ICourseRepository _repo;
    public PublishCourseCommandHandler(ICourseRepository repo) => _repo = repo;
    public async Task Handle(PublishCourseCommand req, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(req.Id, req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Course {req.Id} not found.");
        c.Publish();
        await _repo.UpdateAsync(c, ct);
    }
}