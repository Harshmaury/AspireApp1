using MediatR;
namespace Examination.Application.MarksEntry.Commands;
public sealed record SubmitMarksCommand(Guid TenantId, Guid MarksEntryId) : IRequest;
public sealed record ApproveMarksCommand(Guid TenantId, Guid MarksEntryId, Guid ApprovedBy) : IRequest;
public sealed record PublishMarksCommand(Guid TenantId, Guid MarksEntryId) : IRequest;
