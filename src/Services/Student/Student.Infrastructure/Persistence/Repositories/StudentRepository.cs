using Microsoft.EntityFrameworkCore;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using Student.Domain.Enums;
using Student.Infrastructure.Persistence;
using UMS.SharedKernel.Tenancy;

namespace Student.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _db;
    public StudentRepository(StudentDbContext db, ITenantContext? tenant = null) => _db = db;

    public async Task<StudentAggregate?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Students.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<StudentAggregate?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    public async Task<StudentAggregate?> GetByIdReadOnlyAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students.AnyAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    public async Task<(List<StudentAggregate> Items, int TotalCount)> GetAllAsync(
        Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Students.AsNoTracking().Where(s => s.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StudentStatus>(status, true, out var parsed))
            query = query.Where(s => s.Status == parsed);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(StudentAggregate student, CancellationToken ct = default)
    {
        await _db.Students.AddAsync(student, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(StudentAggregate student, CancellationToken ct = default)
    {
        if (_db.Entry(student).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"StudentAggregate '{student.Id}' is detached. " +
                "Command handlers must use GetByIdAsync (tracked). " +
                "Never pass an entity loaded via GetByIdReadOnlyAsync or GetAllAsync into UpdateAsync.");
        await _db.SaveChangesAsync(ct);
    }
}

