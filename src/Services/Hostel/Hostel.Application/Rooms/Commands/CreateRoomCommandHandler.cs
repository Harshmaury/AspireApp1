using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Hostel.Domain.Exceptions;
using MediatR;
namespace Hostel.Application.Rooms.Commands;
public sealed class CreateRoomCommandHandler(IHostelUnitOfWork uow) : IRequestHandler<CreateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        var hostelExists = await uow.Hostels.GetByIdAsync(cmd.HostelId, cmd.TenantId, ct)
            ?? throw new HostelDomainException("HOSTEL_NOT_FOUND", $"Hostel {cmd.HostelId} not found.");
        var exists = await uow.Rooms.RoomNumberExistsAsync(cmd.HostelId, cmd.RoomNumber, cmd.TenantId, ct);
        if (exists) throw new HostelDomainException("ROOM_EXISTS", $"Room {cmd.RoomNumber} already exists in this hostel.");
        var room = Room.Create(cmd.TenantId, cmd.HostelId, cmd.RoomNumber, cmd.Floor, cmd.Type, cmd.Capacity);
        await uow.Rooms.AddAsync(room, ct);
        await uow.SaveChangesAsync(ct);
        return new RoomDto(room.Id, room.HostelId, room.RoomNumber, room.Floor, room.Type, room.Capacity, room.CurrentOccupancy, room.Status);
    }
}
