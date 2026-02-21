using Hostel.Application.DTOs;
using MediatR;
namespace Hostel.Application.Hostels.Queries;
public sealed record GetHostelByIdQuery(Guid HostelId, Guid TenantId) : IRequest<HostelDto?>;
public sealed class GetHostelByIdQueryHandler(Hostel.Application.Interfaces.IHostelUnitOfWork uow)
    : IRequestHandler<GetHostelByIdQuery, HostelDto?>
{
    public async Task<HostelDto?> Handle(GetHostelByIdQuery q, CancellationToken ct)
    {
        var h = await uow.Hostels.GetByIdAsync(q.HostelId, q.TenantId, ct);
        return h is null ? null : new HostelDto(h.Id, h.TenantId, h.Name, h.Type, h.TotalRooms, h.WardenName, h.WardenContact, h.IsActive, h.CreatedAt);
    }
}
