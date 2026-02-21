using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using MediatR;
namespace Hostel.Application.Rooms.Queries;
public sealed record GetRoomsQuery(Guid HostelId, Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<RoomDto>>;
public sealed class GetRoomsQueryHandler(IHostelUnitOfWork uow) : IRequestHandler<GetRoomsQuery, PagedResult<RoomDto>>
{
    public async Task<PagedResult<RoomDto>> Handle(GetRoomsQuery q, CancellationToken ct)
    {
        var total = await uow.Rooms.CountByHostelAsync(q.HostelId, q.TenantId, ct);
        var items = await uow.Rooms.GetByHostelAsync(q.HostelId, q.TenantId, q.Page, q.PageSize, ct);
        var dtos = items.Select(r => new RoomDto(r.Id, r.HostelId, r.RoomNumber, r.Floor, r.Type, r.Capacity, r.CurrentOccupancy, r.Status)).ToList();
        return new PagedResult<RoomDto>(dtos, total, q.Page, q.PageSize);
    }
}
