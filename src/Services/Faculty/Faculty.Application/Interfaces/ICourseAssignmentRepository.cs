using CourseAssignmentEntity = Faculty.Domain.Entities.CourseAssignment;
namespace Faculty.Application.Interfaces;
public interface ICourseAssignmentRepository
{
    Task<CourseAssignmentEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<CourseAssignmentEntity>> GetByFacultyAsync(Guid facultyId, Guid tenantId, CancellationToken ct = default);
    Task<List<CourseAssignmentEntity>> GetByFacultyAndYearAsync(Guid facultyId, string academicYear, Guid tenantId, CancellationToken ct = default);
    Task<List<CourseAssignmentEntity>> GetByCourseAsync(Guid courseId, string academicYear, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid facultyId, Guid courseId, string academicYear, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(CourseAssignmentEntity assignment, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
