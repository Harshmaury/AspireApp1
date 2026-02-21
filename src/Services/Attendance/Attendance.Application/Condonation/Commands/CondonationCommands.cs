using MediatR;
namespace Attendance.Application.Condonation.Commands;
public sealed record CreateCondonationRequestCommand(Guid TenantId, Guid StudentId, Guid CourseId, string Reason, string? DocumentUrl = null) : IRequest<Guid>;
public sealed record ApproveCondonationCommand(Guid TenantId, Guid RequestId, Guid ReviewedBy, string? Note = null) : IRequest;
public sealed record RejectCondonationCommand(Guid TenantId, Guid RequestId, Guid ReviewedBy, string Note) : IRequest;
