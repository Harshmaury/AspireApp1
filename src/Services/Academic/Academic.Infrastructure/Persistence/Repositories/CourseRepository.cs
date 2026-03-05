using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Academic.Infrastructure.Persistence.Repositories;

internal sealed class CourseRepository : ICourseRepository
{
    private readonly AcademicDbContext _db;
    public CourseRepository(AcademicDbContext db) => _db = db;

    public async Task<Course?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public async Task<bool> ExistsAsync(string code, Guid tenantId, CancellationToken ct = default)
        => await _db.Courses.AnyAsync(c => c.Code == code.ToUpperInvariant() && c.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Course>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default)
        => await _db.Courses.Where(c => c.DepartmentId == departmentId && c.TenantId == tenantId).OrderBy(c => c.Code).ToListAsync(ct);

    public async Task AddAsync(Course course, CancellationToken ct = default)
    {
        await _db.Courses.AddAsync(course, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Course course, CancellationToken ct = default)
    {
        // FIX A2: Never call _db.Update(entity). If entity is detached, domain events
        // are lost. Guard here so the bug surfaces immediately at the call site.
        // Pattern copied from Faculty.Infrastructure (gold standard for this codebase).
        if (_db.Entry(course).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached Course (Id={course.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");

        await _db.SaveChangesAsync(ct);
    }
}
