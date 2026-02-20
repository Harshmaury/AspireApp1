using MediatR;
using Student.Application.Interfaces;

namespace Student.Application.Features.Students.Commands;

public sealed record AdmitStudentCommand(Guid StudentId, Guid TenantId) : IRequest<bool>;

public sealed class AdmitStudentCommandHandler
    : IRequestHandler<AdmitStudentCommand, bool>
{
    private readonly IStudentRepository _repository;

    public AdmitStudentCommandHandler(IStudentRepository repository)
        => _repository = repository;

    public async Task<bool> Handle(AdmitStudentCommand request, CancellationToken ct)
    {
        var student = await _repository.GetByIdAsync(request.StudentId, request.TenantId, ct)
            ?? throw new InvalidOperationException("Student not found.");

        student.Admit();
        await _repository.UpdateAsync(student, ct);
        return true;
    }
}
