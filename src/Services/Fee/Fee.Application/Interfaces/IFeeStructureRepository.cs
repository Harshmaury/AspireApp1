using FeeStructureEntity = Fee.Domain.Entities.FeeStructure;
namespace Fee.Application.Interfaces;
public interface IFeeStructureRepository
{
    Task<FeeStructureEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<FeeStructureEntity?> GetByProgrammeSemesterAsync(Guid programmeId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default);
    Task<List<FeeStructureEntity>> GetByProgrammeAsync(Guid programmeId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(FeeStructureEntity feeStructure, CancellationToken ct = default);
    Task UpdateAsync(FeeStructureEntity feeStructure, CancellationToken ct = default);
}
