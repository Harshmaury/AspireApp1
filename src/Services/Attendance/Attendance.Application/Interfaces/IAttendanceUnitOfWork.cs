// IAttendanceUnitOfWork.cs  — created by Fix-P9-AttendanceUoW.ps1
// FIX ATT-2: wraps both repositories so MarkAttendanceCommandHandler can flush
// AttendanceRecord + AttendanceSummary in a single database round trip.
// Pattern copied from Hostel.Application.Interfaces.IHostelUnitOfWork.
using Attendance.Application.Interfaces;

namespace Attendance.Application.Interfaces;

public interface IAttendanceUnitOfWork
{
    /// <summary>Do NOT call SaveChangesAsync on this repo directly — use the UoW flush.</summary>
    IAttendanceRecordRepository Records { get; }

    /// <summary>Do NOT call SaveChangesAsync on this repo directly — use the UoW flush.</summary>
    IAttendanceSummaryRepository Summaries { get; }

    /// <summary>Commits both repositories atomically in one round trip.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
