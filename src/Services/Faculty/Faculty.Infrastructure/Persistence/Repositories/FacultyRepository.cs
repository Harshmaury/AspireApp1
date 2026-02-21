using Faculty.Application.Interfaces;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
using Microsoft.EntityFrameworkCore;

namespace Faculty.Infrastructure.Persistence.Repositories;

public sealed class FacultyRepository : IFacultyRepository
{
    private readonly FacultyDbContext _db;

    public FacultyRepository(FacultyDbContext db) => _db = db;

    public async Task<FacultyEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

    public async Task<FacultyEntity?> GetByEmployeeIdAsync(string employeeId, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.FirstOrDefaultAsync(f => f.EmployeeId == employeeId.ToUpper() && f.TenantId == tenantId, ct);

    public async Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.AnyAsync(f => f.EmployeeId == code && f.TenantId == tenantId, ct);

    public async Task AddAsync(FacultyEntity faculty, CancellationToken ct = default)
        => await _db.Faculty.AddAsync(faculty, ct);

    public async Task UpdateAsync(FacultyEntity faculty, CancellationToken ct = default)
        => _db.Faculty.Update(faculty);

    public async Task<IReadOnlyList<FacultyEntity>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Faculty.Where(f => f.TenantId == tenantId).ToListAsync(ct);
}
