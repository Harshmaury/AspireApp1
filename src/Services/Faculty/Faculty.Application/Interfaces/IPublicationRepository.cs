using PublicationEntity = Faculty.Domain.Entities.Publication;
namespace Faculty.Application.Interfaces;
public interface IPublicationRepository
{
    Task<PublicationEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<PublicationEntity>> GetByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default);
    Task<List<PublicationEntity>> GetByTypeAsync(string type, Guid tenantId, CancellationToken ct = default);
    Task<int> GetCountByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(PublicationEntity publication, CancellationToken ct = default);
    Task UpdateAsync(PublicationEntity publication, CancellationToken ct = default);
}
