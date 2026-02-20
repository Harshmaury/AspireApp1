using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Department.Commands;
public sealed record CreateDepartmentCommand(Guid TenantId, string Name, string Code, int EstablishedYear, string? Description) : IRequest<DepartmentDto>;