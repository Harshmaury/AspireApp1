using Microsoft.EntityFrameworkCore;
using Faculty.Application.Interfaces;
using Faculty.Domain.Entities;
using Faculty.Infrastructure.Persistence;
namespace Faculty.Infrastructure.Persistence.Repositories;
public sealed class CourseAssignmentRepository : ICourseAssignmentRepository
{
    private readonly FacultyDbContext _db;
    public CourseAssignmentRepository(FacultyDbContext db) => _db = db;
    public async Task<CourseAssignment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.CourseAssignments.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);
    public async Task<List<CourseAssignment>> GetByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default)
        => await _db.CourseAssignments.Where(a => a.FacultyId == facultyId && a.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<CourseAssignment>> GetByFacultyAndYearAsync(Guid facultyId, string academicYear, Guid tenantId, CancellationToken ct = default)
        => await _db.CourseAssignments.Where(a => a.FacultyId == facultyId && a.AcademicYear == academicYear && a.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<CourseAssignment>> GetByCourseAsync(Guid courseId, string academicYear, Guid tenantId, CancellationToken ct = default)
        => await _db.CourseAssignments.Where(a => a.CourseId == courseId && a.AcademicYear == academicYear && a.TenantId == tenantId).ToListAsync(ct);
    public async Task<bool> ExistsAsync(Guid facultyId, Guid courseId, string academicYear, Guid tenantId, CancellationToken ct = default)
        => await _db.CourseAssignments.AnyAsync(a => a.FacultyId == facultyId && a.CourseId == courseId && a.AcademicYear == academicYear && a.TenantId == tenantId, ct);
    public async Task AddAsync(CourseAssignment assignment, CancellationToken ct = default)
    {
        await _db.CourseAssignments.AddAsync(assignment, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var a = await GetByIdAsync(id, tenantId, ct);
        if (a is not null) { _db.CourseAssignments.Remove(a); await _db.SaveChangesAsync(ct); }
    }
}
