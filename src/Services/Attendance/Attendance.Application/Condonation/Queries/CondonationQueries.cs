using MediatR;
using Attendance.Application.DTOs;
namespace Attendance.Application.Condonation.Queries;
public sealed record GetPendingCondonationsQuery(Guid TenantId) : IRequest<List<CondonationRequestDto>>;
public sealed record GetStudentCondonationsQuery(Guid StudentId, Guid TenantId) : IRequest<List<CondonationRequestDto>>;
