using ScholarshipEntity = Fee.Domain.Entities.Scholarship;
namespace Fee.Application.Interfaces;
public interface IScholarshipRepository
{
    Task<ScholarshipEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<ScholarshipEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ScholarshipEntity scholarship, CancellationToken ct = default);
    Task UpdateAsync(ScholarshipEntity scholarship, CancellationToken ct = default);
}
