using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Academic.Infrastructure.Persistence.Repositories;
internal sealed class ProgrammeRepository : IProgrammeRepository
{
    private readonly AcademicDbContext _db;
    public ProgrammeRepository(AcademicDbContext db) => _db = db;
    public async Task<Programme?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Programmes.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
    public async Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default)
        => await _db.Programmes.AnyAsync(p => p.Code == code.ToUpperInvariant() && p.TenantId == tenantId, ct);
    public async Task<IReadOnlyList<Programme>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default)
        => await _db.Programmes.Where(p => p.DepartmentId == departmentId && p.TenantId == tenantId).OrderBy(p => p.Name).ToListAsync(ct);
    public async Task AddAsync(Programme programme, CancellationToken ct = default)
    {
        await _db.Programmes.AddAsync(programme, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(Programme programme, CancellationToken ct = default)
    {
        _db.Programmes.Update(programme);
        await _db.SaveChangesAsync(ct);
    }
}