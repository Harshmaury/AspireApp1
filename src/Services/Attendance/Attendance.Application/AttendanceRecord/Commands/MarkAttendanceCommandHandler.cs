using MediatR;
using Attendance.Application.Interfaces;
using Attendance.Domain.Enums;
using AttendanceRecordEntity = Attendance.Domain.Entities.AttendanceRecord;
using AttendanceSummaryEntity = Attendance.Domain.Entities.AttendanceSummary;
namespace Attendance.Application.AttendanceRecord.Commands;
public sealed class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, Guid>
{
    private readonly IAttendanceRecordRepository _recordRepository;
    private readonly IAttendanceSummaryRepository _summaryRepository;
    public MarkAttendanceCommandHandler(IAttendanceRecordRepository recordRepository, IAttendanceSummaryRepository summaryRepository)
    {
        _recordRepository = recordRepository;
        _summaryRepository = summaryRepository;
    }
    public async Task<Guid> Handle(MarkAttendanceCommand cmd, CancellationToken ct)
    {
        var classType = Enum.Parse<ClassType>(cmd.ClassType, true);
        var record = AttendanceRecordEntity.Create(cmd.TenantId, cmd.StudentId, cmd.CourseId, cmd.AcademicYear, cmd.Semester, cmd.Date, classType, cmd.IsPresent, cmd.MarkedBy);
        await _recordRepository.AddAsync(record, ct);
        // Refresh summary
        var allRecords = await _recordRepository.GetByStudentCourseAsync(cmd.StudentId, cmd.CourseId, cmd.TenantId, ct);
        var summary = await _summaryRepository.GetByStudentCourseAsync(cmd.StudentId, cmd.CourseId, cmd.TenantId, ct);
        if (summary is null)
        {
            summary = AttendanceSummaryEntity.Create(cmd.TenantId, cmd.StudentId, cmd.CourseId, cmd.AcademicYear, cmd.Semester);
            await _summaryRepository.AddAsync(summary, ct);
        }
        summary.Refresh(allRecords.Count, allRecords.Count(r => r.IsPresent));
        await _summaryRepository.UpdateAsync(summary, ct);
        return record.Id;
    }
}
