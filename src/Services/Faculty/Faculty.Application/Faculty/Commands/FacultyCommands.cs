using MediatR;
using Faculty.Domain.Enums;
namespace Faculty.Application.Faculty.Commands;
public sealed record CreateFacultyCommand(Guid TenantId, Guid UserId, Guid DepartmentId, string EmployeeId, string FirstName, string LastName, string Email, string Designation, string Specialization, string HighestQualification, int ExperienceYears, bool IsPhD, DateOnly JoiningDate) : IRequest<Guid>;
public sealed record UpdateFacultyDesignationCommand(Guid TenantId, Guid FacultyId, string Designation) : IRequest;
public sealed record UpdateFacultyStatusCommand(Guid TenantId, Guid FacultyId, string Status) : IRequest;
public sealed record UpdateFacultyExperienceCommand(Guid TenantId, Guid FacultyId, int ExperienceYears) : IRequest;
