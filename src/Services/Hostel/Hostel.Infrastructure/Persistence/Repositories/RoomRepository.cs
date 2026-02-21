using Hostel.Application.Interfaces;
using Hostel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Hostel.Infrastructure.Persistence.Repositories;
public sealed class RoomRepository(HostelDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.Rooms.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<List<Room>> GetByHostelAsync(Guid hostelId, Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        db.Rooms.Where(x => x.HostelId == hostelId && x.TenantId == tenantId)
            .OrderBy(x => x.RoomNumber).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    public Task<int> CountByHostelAsync(Guid hostelId, Guid tenantId, CancellationToken ct = default) =>
        db.Rooms.CountAsync(x => x.HostelId == hostelId && x.TenantId == tenantId, ct);
    public Task<bool> RoomNumberExistsAsync(Guid hostelId, string roomNumber, Guid tenantId, CancellationToken ct = default) =>
        db.Rooms.AnyAsync(x => x.HostelId == hostelId && x.RoomNumber == roomNumber.ToUpper() && x.TenantId == tenantId, ct);
    public async Task AddAsync(Room room, CancellationToken ct = default) => await db.Rooms.AddAsync(room, ct);
    public void Update(Room room) => db.Rooms.Update(room);
}
