using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
namespace Hostel.Application.Interfaces;
public interface IComplaintRepository
{
    Task<HostelComplaint?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<HostelComplaint>> GetAllAsync(Guid tenantId, ComplaintStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, ComplaintStatus? status, CancellationToken ct = default);
    Task<List<HostelComplaint>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(HostelComplaint complaint, CancellationToken ct = default);
    void Update(HostelComplaint complaint);
}
