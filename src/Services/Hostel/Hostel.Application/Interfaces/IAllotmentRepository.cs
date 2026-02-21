using Hostel.Domain.Entities;
namespace Hostel.Application.Interfaces;
public interface IAllotmentRepository
{
    Task<RoomAllotment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<RoomAllotment?> GetActiveByStudentAsync(Guid studentId, string academicYear, Guid tenantId, CancellationToken ct = default);
    Task<List<RoomAllotment>> GetByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default);
    Task<int> CountActiveByRoomAsync(Guid roomId, Guid tenantId, string academicYear, CancellationToken ct = default);
    Task<List<RoomAllotment>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(RoomAllotment allotment, CancellationToken ct = default);
    void Update(RoomAllotment allotment);
}
