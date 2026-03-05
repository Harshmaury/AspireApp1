using MediatR;
using Student.Application.Interfaces;

namespace Student.Application.Features.Students.Commands;

public sealed record EnrollStudentCommand(Guid StudentId, Guid TenantId) : IRequest;
public sealed record SuspendStudentCommand(Guid StudentId, Guid TenantId, string Reason) : IRequest;
public sealed record ReinstateStudentCommand(Guid StudentId, Guid TenantId) : IRequest;
public sealed record GraduateStudentCommand(Guid StudentId, Guid TenantId) : IRequest;
public sealed record ArchiveStudentCommand(Guid StudentId, Guid TenantId) : IRequest;

public sealed record UpdateStudentCommand(
    Guid StudentId, Guid TenantId,
    string FirstName, string LastName, string Email) : IRequest;

public sealed class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand>
{
    private readonly IStudentRepository _repo;
    public EnrollStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(EnrollStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.Enroll();
        await _repo.UpdateAsync(s, ct);
    }
}

public sealed class SuspendStudentCommandHandler : IRequestHandler<SuspendStudentCommand>
{
    private readonly IStudentRepository _repo;
    public SuspendStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(SuspendStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.Suspend(cmd.Reason);
        await _repo.UpdateAsync(s, ct);
    }
}

public sealed class ReinstateStudentCommandHandler : IRequestHandler<ReinstateStudentCommand>
{
    private readonly IStudentRepository _repo;
    public ReinstateStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(ReinstateStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.Reinstate();
        await _repo.UpdateAsync(s, ct);
    }
}

public sealed class GraduateStudentCommandHandler : IRequestHandler<GraduateStudentCommand>
{
    private readonly IStudentRepository _repo;
    public GraduateStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(GraduateStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.Graduate();
        await _repo.UpdateAsync(s, ct);
    }
}

public sealed class ArchiveStudentCommandHandler : IRequestHandler<ArchiveStudentCommand>
{
    private readonly IStudentRepository _repo;
    public ArchiveStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(ArchiveStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.Archive();
        await _repo.UpdateAsync(s, ct);
    }
}

public sealed class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand>
{
    private readonly IStudentRepository _repo;
    public UpdateStudentCommandHandler(IStudentRepository repo) => _repo = repo;
    public async Task Handle(UpdateStudentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.StudentId, cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");
        s.UpdateDetails(cmd.FirstName, cmd.LastName, cmd.Email);
        await _repo.UpdateAsync(s, ct);
    }
}
