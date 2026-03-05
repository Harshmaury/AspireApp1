using Attendance.Application.Interfaces;
using Attendance.Domain.Enums;
using MediatR;
using AttendanceRecordEntity = Attendance.Domain.Entities.AttendanceRecord;
using AttendanceSummaryEntity = Attendance.Domain.Entities.AttendanceSummary;

namespace Attendance.Application.AttendanceRecord.Commands;

public sealed class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, Guid>
{
    private readonly IAttendanceUnitOfWork _uow;

    public MarkAttendanceCommandHandler(IAttendanceUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(MarkAttendanceCommand cmd, CancellationToken ct)
    {
        var classType = Enum.Parse<ClassType>(cmd.ClassType, true);
        var record = AttendanceRecordEntity.Create(
            cmd.TenantId, cmd.StudentId, cmd.CourseId,
            cmd.AcademicYear, cmd.Semester, cmd.Date,
            classType, cmd.IsPresent, cmd.MarkedBy);

        // FIX ATT-2: Stage record (no SaveChanges yet)
        await _uow.Records.AddAsync(record, ct);

        var (total, attended) = await _uow.Records.GetCountsAsync(
            cmd.StudentId, cmd.CourseId, cmd.TenantId, ct);

        var summary = await _uow.Summaries.GetByStudentCourseAsync(
            cmd.StudentId, cmd.CourseId, cmd.TenantId, ct);

        if (summary is null)
        {
            summary = AttendanceSummaryEntity.Create(
                cmd.TenantId, cmd.StudentId, cmd.CourseId,
                cmd.AcademicYear, cmd.Semester);
            await _uow.Summaries.AddAsync(summary, ct);
        }

        summary.Refresh(total, attended);

        // Single flush — record and summary committed atomically in one round trip.
        // Before this fix, two separate SaveChangesAsync calls left a window where
        // the record could be persisted but the summary not updated (partial write).
        await _uow.SaveChangesAsync(ct);

        return record.Id;
    }
}
