using Examination.Domain.Entities;
namespace Examination.Application.Interfaces;
public interface IResultCardRepository
{
    Task<ResultCard?> GetByStudentSemesterAsync(Guid studentId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default);
    Task<List<ResultCard>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ResultCard resultCard, CancellationToken ct = default);
    Task UpdateAsync(ResultCard resultCard, CancellationToken ct = default);
}
