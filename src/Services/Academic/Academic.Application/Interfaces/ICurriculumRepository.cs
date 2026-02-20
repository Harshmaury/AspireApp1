namespace Academic.Application.Interfaces;
public interface ICurriculumRepository
{
    Task<Academic.Domain.Entities.Curriculum?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Academic.Domain.Entities.Curriculum>> GetByProgrammeAsync(Guid programmeId, Guid tenantId, string version, CancellationToken ct = default);
    Task AddAsync(Academic.Domain.Entities.Curriculum curriculum, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}