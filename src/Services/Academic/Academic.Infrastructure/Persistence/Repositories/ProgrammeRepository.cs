using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Academic.Infrastructure.Persistence.Repositories;

internal sealed class ProgrammeRepository : IProgrammeRepository
{
    private readonly AcademicDbContext _db;
    public ProgrammeRepository(AcademicDbContext db, ITenantContext? tenant = null) => _db = db;

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
        if (_db.Entry(programme).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached Programme (Id={programme.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _db.SaveChangesAsync(ct);
    }
}

