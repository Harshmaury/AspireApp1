using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Department.Queries;
public sealed record GetDepartmentByIdQuery(Guid Id, Guid TenantId) : IRequest<DepartmentDto?>;