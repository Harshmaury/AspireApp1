namespace Academic.Application.Interfaces;
public interface ICourseRepository
{
    Task<Academic.Domain.Entities.Course?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Academic.Domain.Entities.Course>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Academic.Domain.Entities.Course course, CancellationToken ct = default);
    Task UpdateAsync(Academic.Domain.Entities.Course course, CancellationToken ct = default);
}