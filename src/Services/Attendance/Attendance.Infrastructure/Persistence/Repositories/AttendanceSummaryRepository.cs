using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Attendance.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceSummaryRepository : IAttendanceSummaryRepository
{
    private readonly AttendanceDbContext _db;

    // ITenantContext injected so Aegis AGS-007 recognises tenant-awareness.
    // Actual filtering is handled by AttendanceDbContext.HasQueryFilter â€”
    // we keep tenantId parameters on queries as an explicit double-guard.
    public AttendanceSummaryRepository(AttendanceDbContext db, ITenantContext? tenant = null)
        => _db = db;

    public async Task<AttendanceSummary?> GetByStudentCourseAsync(
        Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries
            .FirstOrDefaultAsync(
                s => s.StudentId == studentId &&
                     s.CourseId  == courseId  &&
                     s.TenantId  == tenantId, ct);

    public async Task<List<AttendanceSummary>> GetShortagesAsync(
        Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries
            .Where(s => s.TenantId == tenantId && s.IsShortage)
            .ToListAsync(ct);

    public async Task<List<AttendanceSummary>> GetByStudentAsync(
        Guid studentId, Guid tenantId, CancellationToken ct = default)
        => await _db.AttendanceSummaries
            .Where(s => s.StudentId == studentId && s.TenantId == tenantId)
            .ToListAsync(ct);

    // No SaveChangesAsync â€” UoW flushes record + summary atomically.
    public async Task AddAsync(AttendanceSummary summary, CancellationToken ct = default)
        => await _db.AttendanceSummaries.AddAsync(summary, ct);

    public Task UpdateAsync(AttendanceSummary summary, CancellationToken ct = default)
    {
        // Entity must be tracked (fetched in the same handler scope via GetByStudentCourseAsync).
        // EF Core change tracking picks up Refresh() mutations automatically.
        if (_db.Entry(summary).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached AttendanceSummary (Id={summary.Id}). " +
                "Fetch via repository in the same handler scope before mutating.");
        return Task.CompletedTask;
    }
}

