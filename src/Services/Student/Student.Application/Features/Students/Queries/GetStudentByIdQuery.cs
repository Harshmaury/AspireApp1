using MediatR;
using Student.Application.Interfaces;

namespace Student.Application.Features.Students.Queries;

public sealed record GetStudentByIdQuery(Guid StudentId, Guid TenantId)
    : IRequest<StudentDto?>;

public sealed class GetStudentByIdQueryHandler
    : IRequestHandler<GetStudentByIdQuery, StudentDto?>
{
    private readonly IStudentRepository _repository;

    public GetStudentByIdQueryHandler(IStudentRepository repository)
        => _repository = repository;

    public async Task<StudentDto?> Handle(GetStudentByIdQuery request, CancellationToken ct)
    {
        // GetByIdReadOnlyAsync: AsNoTracking - query handlers never mutate.
        // Do NOT use GetByIdAsync here - that is the tracked write path
        // reserved for command handlers only.
        var s = await _repository.GetByIdReadOnlyAsync(request.StudentId, request.TenantId, ct);
        if (s is null) return null;

        return new StudentDto(
            s.Id, s.TenantId, s.UserId,
            s.FirstName, s.LastName, s.Email,
            s.StudentNumber, s.Status.ToString(),
            s.CreatedAt, s.UpdatedAt,
            s.AdmittedAt, s.EnrolledAt,
            s.GraduatedAt, s.SuspensionReason);
    }
}