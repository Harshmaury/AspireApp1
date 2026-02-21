using Hostel.Application.Interfaces;
using Hostel.Domain.Exceptions;
using MediatR;
namespace Hostel.Application.Hostels.Commands;
public sealed class UpdateWardenCommandHandler(IHostelUnitOfWork uow) : IRequestHandler<UpdateWardenCommand>
{
    public async Task Handle(UpdateWardenCommand cmd, CancellationToken ct)
    {
        var hostel = await uow.Hostels.GetByIdAsync(cmd.HostelId, cmd.TenantId, ct)
            ?? throw new HostelDomainException("HOSTEL_NOT_FOUND", $"Hostel {cmd.HostelId} not found.");
        hostel.UpdateWarden(cmd.WardenName, cmd.WardenContact);
        uow.Hostels.Update(hostel);
        await uow.SaveChangesAsync(ct);
    }
}
