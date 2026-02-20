using MediatR;
using Student.Application.Interfaces;

namespace Student.Application.Features.Students.Queries;

public sealed record GetStudentByIdQuery(Guid StudentId, Guid TenantId)
    : IRequest<StudentDto?>;

public sealed record StudentDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string StudentNumber,
    string Status,
    DateTime CreatedAt);

public sealed class GetStudentByIdQueryHandler
    : IRequestHandler<GetStudentByIdQuery, StudentDto?>
{
    private readonly IStudentRepository _repository;

    public GetStudentByIdQueryHandler(IStudentRepository repository)
        => _repository = repository;

    public async Task<StudentDto?> Handle(GetStudentByIdQuery request, CancellationToken ct)
    {
        var student = await _repository.GetByIdAsync(request.StudentId, request.TenantId, ct);
        if (student is null) return null;

        return new StudentDto(
            student.Id,
            student.TenantId,
            student.UserId,
            student.FirstName,
            student.LastName,
            student.Email,
            student.StudentNumber,
            student.Status.ToString(),
            student.CreatedAt);
    }
}
