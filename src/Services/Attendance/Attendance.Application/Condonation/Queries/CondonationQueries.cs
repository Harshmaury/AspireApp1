using Attendance.Application.DTOs;
using MediatR;
namespace Attendance.Application.Condonation.Queries;
public sealed record GetPendingCondonationsQuery(Guid TenantId) : IRequest<List<CondonationRequestDto>>;
public sealed record GetStudentCondonationsQuery(Guid StudentId, Guid TenantId) : IRequest<List<CondonationRequestDto>>;
