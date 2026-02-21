using Microsoft.EntityFrameworkCore;
using Faculty.Application.Interfaces;
using Faculty.Domain.Entities;
using Faculty.Domain.Enums;
using Faculty.Infrastructure.Persistence;
namespace Faculty.Infrastructure.Persistence.Repositories;
public sealed class PublicationRepository : IPublicationRepository
{
    private readonly FacultyDbContext _db;
    public PublicationRepository(FacultyDbContext db) => _db = db;
    public async Task<Publication?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Publications.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
    public async Task<List<Publication>> GetByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default)
        => await _db.Publications.Where(p => p.FacultyId == facultyId && p.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<Publication>> GetByTypeAsync(string type, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(type))
            return await _db.Publications.Where(p => p.TenantId == tenantId).ToListAsync(ct);
        if (!Enum.TryParse<PublicationType>(type, true, out var pubType))
            return new List<Publication>();
        return await _db.Publications.Where(p => p.TenantId == tenantId && p.Type == pubType).ToListAsync(ct);
    }
    public async Task<int> GetCountByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default)
        => await _db.Publications.CountAsync(p => p.FacultyId == facultyId && p.TenantId == tenantId, ct);
    public async Task AddAsync(Publication publication, CancellationToken ct = default)
    {
        await _db.Publications.AddAsync(publication, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(Publication publication, CancellationToken ct = default)
    {
        _db.Publications.Update(publication);
        await _db.SaveChangesAsync(ct);
    }
}
