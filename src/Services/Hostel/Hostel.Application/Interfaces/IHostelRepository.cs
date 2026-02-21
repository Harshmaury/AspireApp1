using HostelEntity = Hostel.Domain.Entities.Hostel;
namespace Hostel.Application.Interfaces;
public interface IHostelRepository
{
    Task<HostelEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<HostelEntity>> GetAllAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(HostelEntity hostel, CancellationToken ct = default);
    void Update(HostelEntity hostel);
}
