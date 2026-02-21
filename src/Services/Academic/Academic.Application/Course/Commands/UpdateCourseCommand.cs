using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Course.Commands;
public sealed record UpdateCourseCommand(Guid Id, Guid TenantId, string Name, string? Description, int Credits, string CourseType, int MaxEnrollment) : IRequest;
public sealed class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand>
{
    private readonly ICourseRepository _repo;
    public UpdateCourseCommandHandler(ICourseRepository repo) => _repo = repo;
    public async Task Handle(UpdateCourseCommand req, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(req.Id, req.TenantId, ct)
            ?? throw new KeyNotFoundException($"Course {req.Id} not found.");
        c.Update(req.Name, req.Description, req.Credits, req.CourseType, req.MaxEnrollment);
        await _repo.UpdateAsync(c, ct);
    }
}