using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    // FIX ATT-2: No SaveChangesAsync here — when used via IAttendanceUnitOfWork,
    // the UoW flushes both record and summary atomically in one round trip.
    public async Task AddAsync(AttendanceSummary summary, CancellationToken ct = default)
        => await _db.AttendanceSummaries.AddAsync(summary, ct);

    public Task UpdateAsync(AttendanceSummary summary, CancellationToken ct = default)
    {
        // Entity is already tracked from GetByStudentCourseAsync in the same scope.
        // EF Core change detection picks up Refresh() mutations automatically.
        if (_db.Entry(summary).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached AttendanceSummary (Id={summary.Id}). " +
                "Fetch via repository in the same handler scope before mutating.");
        return Task.CompletedTask;
    }
}
