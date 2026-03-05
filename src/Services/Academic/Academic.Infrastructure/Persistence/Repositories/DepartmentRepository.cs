using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Academic.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly AcademicDbContext _db;
    public DepartmentRepository(AcademicDbContext db) => _db = db;

    public async Task<Department?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

    public async Task<Department?> GetByCodeAsync(string code, Guid tenantId, CancellationToken ct = default)
        => await _db.Departments.FirstOrDefaultAsync(d => d.Code == code.ToUpperInvariant() && d.TenantId == tenantId, ct);

    public async Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default)
        => await _db.Departments.AnyAsync(d => d.Code == code.ToUpperInvariant() && d.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Department>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Departments.Where(d => d.TenantId == tenantId).OrderBy(d => d.Name).ToListAsync(ct);

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        await _db.Departments.AddAsync(department, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        // FIX A2: Never call _db.Update(entity). If entity is detached, domain events
        // are lost. Guard here so the bug surfaces immediately at the call site.
        // Pattern copied from Faculty.Infrastructure (gold standard for this codebase).
        if (_db.Entry(department).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached Department (Id={department.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");

        await _db.SaveChangesAsync(ct);
    }
}
