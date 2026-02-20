namespace Academic.Application.Interfaces;
public interface IDepartmentRepository
{
    Task<Academic.Domain.Entities.Department?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Academic.Domain.Entities.Department?> GetByCodeAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Academic.Domain.Entities.Department>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Academic.Domain.Entities.Department department, CancellationToken ct = default);
    Task UpdateAsync(Academic.Domain.Entities.Department department, CancellationToken ct = default);
}