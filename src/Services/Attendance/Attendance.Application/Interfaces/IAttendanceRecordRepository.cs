using AttendanceRecordEntity = Attendance.Domain.Entities.AttendanceRecord;
namespace Attendance.Application.Interfaces;
public interface IAttendanceRecordRepository
{
    Task<AttendanceRecordEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<AttendanceRecordEntity?> GetByStudentCourseDateAsync(Guid studentId, Guid courseId, DateOnly date, Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceRecordEntity>> GetByStudentCourseAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task<List<AttendanceRecordEntity>> GetByCourseAndDateAsync(Guid courseId, DateOnly date, Guid tenantId, CancellationToken ct = default);
    Task<(int Total, int Attended)> GetCountsAsync(Guid studentId, Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(AttendanceRecordEntity record, CancellationToken ct = default);
    Task UpdateAsync(AttendanceRecordEntity record, CancellationToken ct = default);
}
