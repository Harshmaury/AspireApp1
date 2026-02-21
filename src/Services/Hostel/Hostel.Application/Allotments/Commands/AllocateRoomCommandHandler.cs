using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Hostel.Domain.Exceptions;
using MediatR;
namespace Hostel.Application.Allotments.Commands;
public sealed class AllocateRoomCommandHandler(IHostelUnitOfWork uow) : IRequestHandler<AllocateRoomCommand, AllotmentDto>
{
    public async Task<AllotmentDto> Handle(AllocateRoomCommand cmd, CancellationToken ct)
    {
        // Guard: student must not have an active allotment this year
        var existing = await uow.Allotments.GetActiveByStudentAsync(cmd.StudentId, cmd.AcademicYear, cmd.TenantId, ct);
        if (existing is not null)
            throw new HostelDomainException("ALREADY_ALLOTTED", "Student already has an active room allotment for this academic year.");

        // Guard: room must have capacity
        var room = await uow.Rooms.GetByIdAsync(cmd.RoomId, cmd.TenantId, ct)
            ?? throw new HostelDomainException("ROOM_NOT_FOUND", $"Room {cmd.RoomId} not found.");

        // Room domain method will throw if full or under maintenance
        room.IncrementOccupancy();
        uow.Rooms.Update(room);

        var allotment = RoomAllotment.Create(cmd.TenantId, cmd.StudentId, cmd.RoomId, cmd.HostelId, cmd.AcademicYear, cmd.BedNumber);
        await uow.Allotments.AddAsync(allotment, ct);
        await uow.SaveChangesAsync(ct);
        return new AllotmentDto(allotment.Id, allotment.StudentId, allotment.RoomId, allotment.HostelId,
            allotment.AcademicYear, allotment.BedNumber, allotment.Status, allotment.AllottedAt, allotment.VacatedAt);
    }
}
