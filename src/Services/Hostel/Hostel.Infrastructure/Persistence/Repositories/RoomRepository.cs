using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Hostel.Infrastructure.Persistence.Repositories;

internal sealed class RoomRepository : IRoomRepository
{
    private readonly HostelDbContext _db;
    public RoomRepository(HostelDbContext db, ITenantContext? tenant = null) => _db = db;

    public Task<Room?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

    public Task<List<Room>> GetByHostelAsync(Guid hostelId, Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        _db.Rooms.Where(x => x.HostelId == hostelId && x.TenantId == tenantId)
            .OrderBy(x => x.RoomNumber).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<int> CountByHostelAsync(Guid hostelId, Guid tenantId, CancellationToken ct = default) =>
        _db.Rooms.CountAsync(x => x.HostelId == hostelId && x.TenantId == tenantId, ct);

    public Task<bool> RoomNumberExistsAsync(Guid hostelId, string roomNumber, Guid tenantId, CancellationToken ct = default) =>
        _db.Rooms.AnyAsync(x => x.HostelId == hostelId && x.RoomNumber == roomNumber.ToUpper() && x.TenantId == tenantId, ct);

    public async Task AddAsync(Room room, CancellationToken ct = default) =>
        await _db.Rooms.AddAsync(room, ct);

    public void Update(Room room) => _db.Rooms.Update(room);
}

