using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using MediatR;
namespace Hostel.Application.Allotments.Queries;
public sealed record GetAllotmentsQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AllotmentDto>>;
public sealed class GetAllotmentsQueryHandler(IHostelUnitOfWork uow) : IRequestHandler<GetAllotmentsQuery, PagedResult<AllotmentDto>>
{
    public async Task<PagedResult<AllotmentDto>> Handle(GetAllotmentsQuery q, CancellationToken ct)
    {
        var total = await uow.Allotments.CountAsync(q.TenantId, ct);
        var items = await uow.Allotments.GetAllAsync(q.TenantId, q.Page, q.PageSize, ct);
        var dtos = items.Select(a => new AllotmentDto(a.Id, a.StudentId, a.RoomId, a.HostelId,
            a.AcademicYear, a.BedNumber, a.Status, a.AllottedAt, a.VacatedAt)).ToList();
        return new PagedResult<AllotmentDto>(dtos, total, q.Page, q.PageSize);
    }
}
