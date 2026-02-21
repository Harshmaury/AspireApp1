using Hostel.Application.DTOs;
using MediatR;
namespace Hostel.Application.Hostels.Queries;
public sealed record GetHostelsQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<HostelDto>>;
public sealed class GetHostelsQueryHandler(Hostel.Application.Interfaces.IHostelUnitOfWork uow)
    : IRequestHandler<GetHostelsQuery, PagedResult<HostelDto>>
{
    public async Task<PagedResult<HostelDto>> Handle(GetHostelsQuery q, CancellationToken ct)
    {
        var total = await uow.Hostels.CountAsync(q.TenantId, ct);
        var items = await uow.Hostels.GetAllAsync(q.TenantId, q.Page, q.PageSize, ct);
        var dtos = items.Select(h => new HostelDto(h.Id, h.TenantId, h.Name, h.Type, h.TotalRooms, h.WardenName, h.WardenContact, h.IsActive, h.CreatedAt)).ToList();
        return new PagedResult<HostelDto>(dtos, total, q.Page, q.PageSize);
    }
}
