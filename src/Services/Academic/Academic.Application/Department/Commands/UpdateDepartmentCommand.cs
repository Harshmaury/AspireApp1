using MediatR;
namespace Academic.Application.Department.Commands;
public sealed record UpdateDepartmentCommand(Guid Id, Guid TenantId, string Name, string? Description) : IRequest;