using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Department.Queries;
public sealed record GetAllDepartmentsQuery(Guid TenantId) : IRequest<IReadOnlyList<DepartmentDto>>;