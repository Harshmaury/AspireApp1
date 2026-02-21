using AttendanceSummaryEntity = Attendance.Domain.Entities.AttendanceSummary;
namespace Attendance.Application.Interfaces;
public interface IAttendanceSummaryRepository
{
    Task<AttendanceSummaryEntity?> GetByStudentCourseAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceSummaryEntity>> GetShortagesAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceSummaryEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(AttendanceSummaryEntity summary, CancellationToken ct = default);
    Task UpdateAsync(AttendanceSummaryEntity summary, CancellationToken ct = default);
}
