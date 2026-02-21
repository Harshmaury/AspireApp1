using Attendance.Domain.Entities;
namespace Attendance.Application.Interfaces;
public interface IAttendanceSummaryRepository
{
    Task<AttendanceSummary?> GetByStudentCourseAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceSummary>> GetShortagesAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceSummary>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(AttendanceSummary summary, CancellationToken ct = default);
    Task UpdateAsync(AttendanceSummary summary, CancellationToken ct = default);
}
