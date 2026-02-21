using Hostel.Domain.Entities;
namespace Hostel.Application.Interfaces;
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<Room>> GetByHostelAsync(Guid hostelId, Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByHostelAsync(Guid hostelId, Guid tenantId, CancellationToken ct = default);
    Task<bool> RoomNumberExistsAsync(Guid hostelId, string roomNumber, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Room room, CancellationToken ct = default);
    void Update(Room room);
}
