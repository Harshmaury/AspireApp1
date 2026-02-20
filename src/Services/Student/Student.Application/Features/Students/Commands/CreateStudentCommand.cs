using MediatR;
using Student.Application.Interfaces;
using Student.Domain.Entities;

namespace Student.Application.Features.Students.Commands;

public sealed record CreateStudentCommand(
    Guid TenantId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email) : IRequest<CreateStudentResult>;

public sealed record CreateStudentResult(Guid StudentId, string StudentNumber, string Status);

public sealed class CreateStudentCommandHandler
    : IRequestHandler<CreateStudentCommand, CreateStudentResult>
{
    private readonly IStudentRepository _repository;

    public CreateStudentCommandHandler(IStudentRepository repository)
        => _repository = repository;

    public async Task<CreateStudentResult> Handle(
        CreateStudentCommand request, CancellationToken ct)
    {
        var exists = await _repository.ExistsAsync(request.UserId, request.TenantId, ct);
        if (exists)
            throw new InvalidOperationException("Student profile already exists for this user.");

        var student = StudentAggregate.Create(
            request.TenantId,
            request.UserId,
            request.FirstName,
            request.LastName,
            request.Email);

        await _repository.AddAsync(student, ct);

        return new CreateStudentResult(student.Id, student.StudentNumber, student.Status.ToString());
    }
}
