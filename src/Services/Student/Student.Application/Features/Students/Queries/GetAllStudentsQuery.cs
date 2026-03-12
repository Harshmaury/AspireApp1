// UMS - University Management System
// Key:     UMS-SHARED-P0-003-RESIDUAL
// Service: Student
// Layer:   Application
using MediatR;
using Student.Application.Interfaces;

namespace Student.Application.Features.Students.Queries;

public sealed record GetAllStudentsQuery(
    Guid    TenantId,
    string? Status   = null,
    int     Page     = 1,
    int     PageSize = 20) : IRequest<PagedResult<StudentDto>>;

public sealed record PagedResult<T>(
    List<T> Items,
    int     TotalCount,
    int     Page,
    int     PageSize,
    int     TotalPages);

public sealed class GetAllStudentsQueryHandler
    : IRequestHandler<GetAllStudentsQuery, PagedResult<StudentDto>>
{
    private readonly IStudentRepository _repo;
    public GetAllStudentsQueryHandler(IStudentRepository repo) => _repo = repo;

    public async Task<PagedResult<StudentDto>> Handle(GetAllStudentsQuery request, CancellationToken ct)
    {
        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (students, total) = await _repo.GetAllAsync(
            request.TenantId, request.Status, page, pageSize, ct);

        var items = students.Select(s => new StudentDto(
            s.Id, s.TenantId, s.UserId,
            s.FirstName, s.LastName, s.Email,
            s.StudentNumber, s.Status.ToString(),
            s.CreatedAt.UtcDateTime, s.UpdatedAt.UtcDateTime,
            s.AdmittedAt, s.EnrolledAt,
            s.GraduatedAt, s.SuspensionReason)).ToList();

        return new PagedResult<StudentDto>(
            items, total, page, pageSize,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
