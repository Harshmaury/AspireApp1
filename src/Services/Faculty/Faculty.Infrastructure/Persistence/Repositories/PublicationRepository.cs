using Faculty.Application.Interfaces;
using Faculty.Domain.Entities;
using Faculty.Domain.Enums;
using Faculty.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Faculty.Infrastructure.Persistence.Repositories;

internal sealed class PublicationRepository : IPublicationRepository
{
    private readonly FacultyDbContext _db;
    public PublicationRepository(FacultyDbContext db, ITenantContext? tenant = null) => _db = db;

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
        if (_db.Entry(publication).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached Publication (Id={publication.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _db.SaveChangesAsync(ct);
    }
}

