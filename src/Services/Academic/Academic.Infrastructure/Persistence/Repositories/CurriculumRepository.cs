using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Academic.Infrastructure.Persistence.Repositories;
internal sealed class CurriculumRepository : ICurriculumRepository
{
    private readonly AcademicDbContext _db;
    public CurriculumRepository(AcademicDbContext db) => _db = db;
    public async Task<Curriculum?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Curricula.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
    public async Task<IReadOnlyList<Curriculum>> GetByProgrammeAsync(Guid programmeId, Guid tenantId, string version, CancellationToken ct = default)
        => await _db.Curricula.Where(c => c.ProgrammeId == programmeId && c.TenantId == tenantId && c.Version == version).OrderBy(c => c.Semester).ToListAsync(ct);
    public async Task AddAsync(Curriculum curriculum, CancellationToken ct = default)
    {
        await _db.Curricula.AddAsync(curriculum, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var c = await _db.Curricula.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (c is not null) { _db.Curricula.Remove(c); await _db.SaveChangesAsync(); }
    }
}