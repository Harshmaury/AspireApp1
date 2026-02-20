using Microsoft.EntityFrameworkCore;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using Student.Infrastructure.Persistence;

namespace Student.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _db;

    public StudentRepository(StudentDbContext db) => _db = db;

    public async Task<StudentAggregate?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<StudentAggregate?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .AnyAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    public async Task AddAsync(StudentAggregate student, CancellationToken ct = default)
    {
        await _db.Students.AddAsync(student, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(StudentAggregate student, CancellationToken ct = default)
    {
        _db.Students.Update(student);
        await _db.SaveChangesAsync(ct);
    }
}
