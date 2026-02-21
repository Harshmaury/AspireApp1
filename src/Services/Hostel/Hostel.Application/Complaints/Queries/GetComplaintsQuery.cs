using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using Hostel.Domain.Enums;
using MediatR;
namespace Hostel.Application.Complaints.Queries;
public sealed record GetComplaintsQuery(Guid TenantId, ComplaintStatus? Status, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ComplaintDto>>;
public sealed class GetComplaintsQueryHandler(IHostelUnitOfWork uow) : IRequestHandler<GetComplaintsQuery, PagedResult<ComplaintDto>>
{
    public async Task<PagedResult<ComplaintDto>> Handle(GetComplaintsQuery q, CancellationToken ct)
    {
        var total = await uow.Complaints.CountAsync(q.TenantId, q.Status, ct);
        var items = await uow.Complaints.GetAllAsync(q.TenantId, q.Status, q.Page, q.PageSize, ct);
        var dtos = items.Select(c => new ComplaintDto(c.Id, c.StudentId, c.HostelId, c.Category,
            c.Description, c.Status, c.ResolutionNote, c.CreatedAt, c.ResolvedAt)).ToList();
        return new PagedResult<ComplaintDto>(dtos, total, q.Page, q.PageSize);
    }
}
