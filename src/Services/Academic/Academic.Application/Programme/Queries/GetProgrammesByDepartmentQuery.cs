using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Programme.Queries;
public sealed record GetProgrammesByDepartmentQuery(Guid DepartmentId, Guid TenantId) : IRequest<IReadOnlyList<ProgrammeDto>>;