using Microsoft.EntityFrameworkCore;
using Attendance.Application.Interfaces;
using Attendance.Infrastructure.Persistence;
using AttendanceRecordEntity = Attendance.Domain.Entities.AttendanceRecord;
namespace Attendance.Infrastructure.Persistence.Repositories;
public sealed class AttendanceRecordRepository : IAttendanceRecordRepository
{
    private readonly AttendanceDbContext _db;
    public AttendanceRecordRepository(AttendanceDbContext db) => _db = db;
    public async Task<AttendanceRecordEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
    public async Task<AttendanceRecordEntity?> GetByStudentCourseDateAsync(Guid studentId, Guid courseId, DateOnly date, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceRecords.FirstOrDefaultAsync(r => r.StudentId == studentId && r.CourseId == courseId && r.Date == date && r.TenantId == tenantId, ct);
    public async Task<List<AttendanceRecordEntity>> GetByStudentCourseAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceRecords.Where(r => r.StudentId == studentId && r.CourseId == courseId && r.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<AttendanceRecordEntity>> GetByCourseAndDateAsync(Guid courseId, DateOnly date, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceRecords.Where(r => r.CourseId == courseId && r.Date == date && r.TenantId == tenantId).ToListAsync(ct);
    public async Task<(int Total, int Attended)> GetCountsAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var counts = await _db.AttendanceRecords
            .Where(r => r.StudentId == studentId && r.CourseId == courseId && r.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Attended = g.Count(r => r.IsPresent) })
            .FirstOrDefaultAsync(ct);
        return counts is null ? (0, 0) : (counts.Total, counts.Attended);
    }
    public async Task AddAsync(AttendanceRecordEntity record, CancellationToken ct = default)
    {
        await _db.AttendanceRecords.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(AttendanceRecordEntity record, CancellationToken ct = default)
    {
        _db.AttendanceRecords.Update(record);
        await _db.SaveChangesAsync(ct);
    }
}
