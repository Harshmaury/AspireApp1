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
        _db.Departments.Update(department);
        await _db.SaveChangesAsync(ct);
    }
}