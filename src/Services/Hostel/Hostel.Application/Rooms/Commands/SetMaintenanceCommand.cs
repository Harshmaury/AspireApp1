using MediatR;
namespace Hostel.Application.Rooms.Commands;
public sealed record SetMaintenanceCommand(Guid RoomId, Guid TenantId, bool Maintenance) : IRequest;
public sealed class SetMaintenanceCommandHandler(Hostel.Application.Interfaces.IHostelUnitOfWork uow) : IRequestHandler<SetMaintenanceCommand>
{
    public async Task Handle(SetMaintenanceCommand cmd, CancellationToken ct)
    {
        var room = await uow.Rooms.GetByIdAsync(cmd.RoomId, cmd.TenantId, ct)
            ?? throw new Hostel.Domain.Exceptions.HostelDomainException("ROOM_NOT_FOUND", $"Room {cmd.RoomId} not found.");
        if (cmd.Maintenance) room.SetMaintenance(); else room.ClearMaintenance();
        uow.Rooms.Update(room);
        await uow.SaveChangesAsync(ct);
    }
}
