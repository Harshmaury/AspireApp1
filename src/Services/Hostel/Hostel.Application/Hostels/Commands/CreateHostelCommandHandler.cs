using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using MediatR;
using HostelEntity = Hostel.Domain.Entities.Hostel;
namespace Hostel.Application.Hostels.Commands;
public sealed class CreateHostelCommandHandler(IHostelUnitOfWork uow)
    : IRequestHandler<CreateHostelCommand, HostelDto>
{
    public async Task<HostelDto> Handle(CreateHostelCommand cmd, CancellationToken ct)
    {
        var hostel = HostelEntity.Create(cmd.TenantId, cmd.Name, cmd.Type, cmd.TotalRooms, cmd.WardenName, cmd.WardenContact);
        await uow.Hostels.AddAsync(hostel, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(hostel);
    }
    private static HostelDto ToDto(HostelEntity h) =>
        new(h.Id, h.TenantId, h.Name, h.Type, h.TotalRooms, h.WardenName, h.WardenContact, h.IsActive, h.CreatedAt);
}
