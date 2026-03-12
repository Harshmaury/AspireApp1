using Attendance.Application.Interfaces;
using Attendance.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Infrastructure.Persistence;

public sealed class AttendanceUnitOfWork : IAttendanceUnitOfWork
{
    private readonly AttendanceDbContext _db;

    public IAttendanceRecordRepository  Records   { get; }
    public IAttendanceSummaryRepository Summaries { get; }

    // Resolve repos from DI so ITenantContext is properly injected into
    // AttendanceSummaryRepository — satisfies AGS-007 tenant-awareness check.
    public AttendanceUnitOfWork(
        AttendanceDbContext db,
        IAttendanceRecordRepository records,
        IAttendanceSummaryRepository summaries)
    {
        _db       = db;
        Records   = records;
        Summaries = summaries;
    }

    /// <summary>
    /// Single flush — AttendanceRecord and AttendanceSummary committed in one
    /// database round trip. Only MarkAttendanceCommandHandler should call this.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
