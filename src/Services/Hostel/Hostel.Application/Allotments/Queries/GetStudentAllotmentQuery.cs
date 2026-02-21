using Hostel.Application.DTOs;
using Hostel.Application.Interfaces;
using MediatR;
namespace Hostel.Application.Allotments.Queries;
public sealed record GetStudentAllotmentQuery(Guid StudentId, string AcademicYear, Guid TenantId) : IRequest<AllotmentDto?>;
public sealed class GetStudentAllotmentQueryHandler(IHostelUnitOfWork uow) : IRequestHandler<GetStudentAllotmentQuery, AllotmentDto?>
{
    public async Task<AllotmentDto?> Handle(GetStudentAllotmentQuery q, CancellationToken ct)
    {
        var a = await uow.Allotments.GetActiveByStudentAsync(q.StudentId, q.AcademicYear, q.TenantId, ct);
        return a is null ? null : new AllotmentDto(a.Id, a.StudentId, a.RoomId, a.HostelId,
            a.AcademicYear, a.BedNumber, a.Status, a.AllottedAt, a.VacatedAt);
    }
}
