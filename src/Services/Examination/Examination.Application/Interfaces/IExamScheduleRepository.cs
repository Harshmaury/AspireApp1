using Examination.Domain.Entities;
using ExamScheduleEntity = Examination.Domain.Entities.ExamSchedule;
namespace Examination.Application.Interfaces;
public interface IExamScheduleRepository
{
    Task<ExamScheduleEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<ExamScheduleEntity>> GetByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ExamScheduleEntity schedule, CancellationToken ct = default);
    Task UpdateAsync(ExamScheduleEntity schedule, CancellationToken ct = default);
}
