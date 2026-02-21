using Hostel.Application.Interfaces;
using Hostel.Infrastructure.Persistence.Repositories;
namespace Hostel.Infrastructure.Persistence;
public sealed class HostelUnitOfWork(HostelDbContext db) : IHostelUnitOfWork
{
    public IHostelRepository Hostels { get; } = new HostelRepository(db);
    public IRoomRepository Rooms { get; } = new RoomRepository(db);
    public IAllotmentRepository Allotments { get; } = new AllotmentRepository(db);
    public IComplaintRepository Complaints { get; } = new ComplaintRepository(db);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
