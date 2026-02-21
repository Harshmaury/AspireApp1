using Microsoft.EntityFrameworkCore;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
namespace Attendance.Infrastructure.Persistence.Repositories;
public sealed class AttendanceSummaryRepository : IAttendanceSummaryRepository
{
    private readonly AttendanceDbContext _db;
    public AttendanceSummaryRepository(AttendanceDbContext db) => _db = db;
    public async Task<AttendanceSummary?> GetByStudentCourseAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries.FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseId == courseId && s.TenantId == tenantId, ct);
    public async Task<List<AttendanceSummary>> GetShortagesAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries.Where(s => s.TenantId == tenantId && s.IsShortage).ToListAsync(ct);
    public async Task<List<AttendanceSummary>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries.Where(s => s.StudentId == studentId && s.TenantId == tenantId).ToListAsync(ct);
    public async Task AddAsync(AttendanceSummary summary, CancellationToken ct = default)
    {
        await _db.AttendanceSummaries.AddAsync(summary, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(AttendanceSummary summary, CancellationToken ct = default)
    {
        _db.AttendanceSummaries.Update(summary);
        await _db.SaveChangesAsync(ct);
    }
}
