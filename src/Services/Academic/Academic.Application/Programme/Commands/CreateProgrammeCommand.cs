using Academic.Application.DTOs;
using MediatR;
namespace Academic.Application.Programme.Commands;
public sealed record CreateProgrammeCommand(Guid TenantId, Guid DepartmentId, string Name, string Code, string Degree, int DurationYears, int TotalCredits, int IntakeCapacity) : IRequest<ProgrammeDto>;