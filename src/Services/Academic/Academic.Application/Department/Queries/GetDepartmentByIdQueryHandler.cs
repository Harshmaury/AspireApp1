using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Department.Queries;
public sealed class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto?>
{
    private readonly IDepartmentRepository _repo;
    public GetDepartmentByIdQueryHandler(IDepartmentRepository repo) => _repo = repo;
    public async Task<DepartmentDto?> Handle(GetDepartmentByIdQuery query, CancellationToken ct)
    {
        var dept = await _repo.GetByIdAsync(query.Id, query.TenantId, ct);
        return dept is null ? null : new DepartmentDto(dept.Id, dept.TenantId, dept.Name, dept.Code, dept.Description, dept.EstablishedYear, dept.Status.ToString(), dept.CreatedAt);
    }
}