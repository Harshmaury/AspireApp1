using MarksEntryEntity = Examination.Domain.Entities.MarksEntry;
namespace Examination.Application.Interfaces;
public interface IMarksEntryRepository
{
    Task<MarksEntryEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<MarksEntryEntity>> GetByExamScheduleAsync(Guid examScheduleId, Guid tenantId, CancellationToken ct = default);
    Task<List<MarksEntryEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(MarksEntryEntity entry, CancellationToken ct = default);
    Task UpdateAsync(MarksEntryEntity entry, CancellationToken ct = default);
}
