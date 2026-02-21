using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using MediatR;
namespace Hostel.Application.Complaints.Commands;
public sealed class RaiseComplaintCommandHandler(IHostelUnitOfWork uow) : IRequestHandler<RaiseComplaintCommand, ComplaintDto>
{
    public async Task<ComplaintDto> Handle(RaiseComplaintCommand cmd, CancellationToken ct)
    {
        var complaint = HostelComplaint.Create(cmd.TenantId, cmd.StudentId, cmd.HostelId, cmd.Category, cmd.Description);
        await uow.Complaints.AddAsync(complaint, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(complaint);
    }
    private static ComplaintDto ToDto(HostelComplaint c) =>
        new(c.Id, c.StudentId, c.HostelId, c.Category, c.Description, c.Status, c.ResolutionNote, c.CreatedAt, c.ResolvedAt);
}
