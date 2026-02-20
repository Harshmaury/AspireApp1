namespace Academic.Application.Interfaces;
public interface IProgrammeRepository
{
    Task<Academic.Domain.Entities.Programme?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Academic.Domain.Entities.Programme>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Academic.Domain.Entities.Programme programme, CancellationToken ct = default);
    Task UpdateAsync(Academic.Domain.Entities.Programme programme, CancellationToken ct = default);
}