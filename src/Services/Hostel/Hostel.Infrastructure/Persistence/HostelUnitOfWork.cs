using Hostel.Application.Interfaces;
using Hostel.Infrastructure.Persistence.Repositories;
using UMS.SharedKernel.Tenancy;

namespace Hostel.Infrastructure.Persistence;

// Repos resolved via DI so ITenantContext flows through each constructor — satisfies AGS-007.
public sealed class HostelUnitOfWork : IHostelUnitOfWork
{
    private readonly HostelDbContext _db;
    public IHostelRepository     Hostels    { get; }
    public IRoomRepository       Rooms      { get; }
    public IAllotmentRepository  Allotments { get; }
    public IComplaintRepository  Complaints { get; }

    public HostelUnitOfWork(
        HostelDbContext db,
        IHostelRepository hostels,
        IRoomRepository rooms,
        IAllotmentRepository allotments,
        IComplaintRepository complaints)
    {
        _db        = db;
        Hostels    = hostels;
        Rooms      = rooms;
        Allotments = allotments;
        Complaints = complaints;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
