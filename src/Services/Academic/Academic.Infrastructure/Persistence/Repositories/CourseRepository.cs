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
        _db.Courses.Update(course);
        await _db.SaveChangesAsync(ct);
    }
}