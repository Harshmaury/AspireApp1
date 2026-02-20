using Academic.Application.DTOs;
using Academic.Application.Interfaces;
using MediatR;
namespace Academic.Application.Department.Queries;
public sealed class GetAllDepartmentsQueryHandler : IRequestHandler<GetAllDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _repo;
    public GetAllDepartmentsQueryHandler(IDepartmentRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetAllDepartmentsQuery query, CancellationToken ct)
    {
        var depts = await _repo.GetAllAsync(query.TenantId, ct);
        return depts.Select(d => new DepartmentDto(d.Id, d.TenantId, d.Name, d.Code, d.Description, d.EstablishedYear, d.Status.ToString(), d.CreatedAt)).ToList();
    }
}