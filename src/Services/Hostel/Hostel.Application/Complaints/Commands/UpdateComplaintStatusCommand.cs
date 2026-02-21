using MediatR;
namespace Hostel.Application.Complaints.Commands;
public sealed record UpdateComplaintStatusCommand(Guid ComplaintId, Guid TenantId,
    string Action, string? ResolutionNote = null) : IRequest;
// Action: "InProgress" | "Resolve" | "Close"
