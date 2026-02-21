using MediatR;
using Faculty.Application.DTOs;
namespace Faculty.Application.Faculty.Queries;
public sealed record GetFacultyByIdQuery(Guid FacultyId, Guid TenantId) : IRequest<FacultyDto?>;
public sealed record GetFacultyByDepartmentQuery(Guid DepartmentId, Guid TenantId) : IRequest<List<FacultyDto>>;
public sealed record GetAllFacultyQuery(Guid TenantId) : IRequest<List<FacultyDto>>;
public sealed record GetFacultyNirfQuery(Guid TenantId) : IRequest<FacultyNirfDto>;
