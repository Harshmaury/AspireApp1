using Attendance.Application.Interfaces;
using Attendance.Infrastructure.Persistence.Repositories;

namespace Attendance.Infrastructure.Persistence;

public sealed class AttendanceUnitOfWork : IAttendanceUnitOfWork
{
    private readonly AttendanceDbContext _db;

    public AttendanceUnitOfWork(AttendanceDbContext db)
    {
        _db       = db;
        Records   = new AttendanceRecordRepository(db);
        Summaries = new AttendanceSummaryRepository(db);
    }

    public IAttendanceRecordRepository  Records   { get; }
    public IAttendanceSummaryRepository Summaries { get; }

    /// <summary>
    /// Single flush — AttendanceRecord and AttendanceSummary committed in one
    /// database round trip. Only MarkAttendanceCommandHandler should call this.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
