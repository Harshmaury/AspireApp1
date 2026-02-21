using Hostel.Application.Interfaces;
using Hostel.Domain.Exceptions;
using MediatR;
namespace Hostel.Application.Complaints.Commands;
public sealed class UpdateComplaintStatusCommandHandler(IHostelUnitOfWork uow) : IRequestHandler<UpdateComplaintStatusCommand>
{
    public async Task Handle(UpdateComplaintStatusCommand cmd, CancellationToken ct)
    {
        var complaint = await uow.Complaints.GetByIdAsync(cmd.ComplaintId, cmd.TenantId, ct)
            ?? throw new HostelDomainException("COMPLAINT_NOT_FOUND", "Complaint not found.");
        switch (cmd.Action)
        {
            case "InProgress": complaint.MarkInProgress(); break;
            case "Resolve":
                if (string.IsNullOrWhiteSpace(cmd.ResolutionNote))
                    throw new HostelDomainException("NOTE_REQUIRED", "Resolution note is required.");
                complaint.Resolve(cmd.ResolutionNote!); break;
            case "Close": complaint.Close(); break;
            default: throw new HostelDomainException("INVALID_ACTION", $"Unknown action: {cmd.Action}");
        }
        uow.Complaints.Update(complaint);
        await uow.SaveChangesAsync(ct);
    }
}
