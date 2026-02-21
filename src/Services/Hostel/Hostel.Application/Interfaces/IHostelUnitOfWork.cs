namespace Hostel.Application.Interfaces;
public interface IHostelUnitOfWork
{
    IHostelRepository Hostels { get; }
    IRoomRepository Rooms { get; }
    IAllotmentRepository Allotments { get; }
    IComplaintRepository Complaints { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
