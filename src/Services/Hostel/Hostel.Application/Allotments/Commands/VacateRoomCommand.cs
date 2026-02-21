using MediatR;
namespace Hostel.Application.Allotments.Commands;
public sealed record VacateRoomCommand(Guid AllotmentId, Guid TenantId) : IRequest;
public sealed class VacateRoomCommandHandler(Hostel.Application.Interfaces.IHostelUnitOfWork uow) : IRequestHandler<VacateRoomCommand>
{
    public async Task Handle(VacateRoomCommand cmd, CancellationToken ct)
    {
        var allotment = await uow.Allotments.GetByIdAsync(cmd.AllotmentId, cmd.TenantId, ct)
            ?? throw new Hostel.Domain.Exceptions.HostelDomainException("ALLOTMENT_NOT_FOUND", "Allotment not found.");
        var room = await uow.Rooms.GetByIdAsync(allotment.RoomId, cmd.TenantId, ct)
            ?? throw new Hostel.Domain.Exceptions.HostelDomainException("ROOM_NOT_FOUND", "Room not found.");
        allotment.Vacate();
        room.DecrementOccupancy();
        uow.Allotments.Update(allotment);
        uow.Rooms.Update(room);
        await uow.SaveChangesAsync(ct);
    }
}
