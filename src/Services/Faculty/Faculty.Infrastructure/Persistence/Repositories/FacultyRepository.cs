using Faculty.Application.Interfaces;
using Faculty.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using FacultyEntity = Faculty.Domain.Entities.Faculty;

namespace Faculty.Infrastructure.Persistence.Repositories;

internal sealed class FacultyRepository : IFacultyRepository
{
    private readonly FacultyDbContext _db;
    public FacultyRepository(FacultyDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task<FacultyEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

    public async Task<FacultyEntity?> GetByEmployeeIdAsync(string employeeId, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.FirstOrDefaultAsync(f => f.EmployeeId == employeeId.ToUpper() && f.TenantId == tenantId, ct);

    public async Task<FacultyEntity?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.FirstOrDefaultAsync(f => f.UserId == userId && f.TenantId == tenantId, ct);

    public async Task<FacultyEntity?> GetByIdReadOnlyAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

    public async Task<List<FacultyEntity>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.AsNoTracking().Where(f => f.DepartmentId == departmentId && f.TenantId == tenantId).ToListAsync(ct);

    public async Task<List<FacultyEntity>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.AsNoTracking().Where(f => f.TenantId == tenantId).ToListAsync(ct);

    public async Task<int> GetPhdCountAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.CountAsync(f => f.TenantId == tenantId && f.IsPhD, ct);

    public async Task AddAsync(FacultyEntity faculty, CancellationToken ct = default)
    {
        await _db.Faculty.AddAsync(faculty, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FacultyEntity faculty, CancellationToken ct = default)
    {
        if (_db.Entry(faculty).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"Faculty '{faculty.Id}' is detached. " +
                "Command handlers must use GetByIdAsync (tracked). " +
                "Never pass an entity from GetByIdReadOnlyAsync or GetAllAsync into UpdateAsync.");
        await _db.SaveChangesAsync(ct);
    }
}

