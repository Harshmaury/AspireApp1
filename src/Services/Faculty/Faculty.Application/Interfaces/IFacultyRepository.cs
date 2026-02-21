using FacultyEntity = Faculty.Domain.Entities.Faculty;
namespace Faculty.Application.Interfaces;
public interface IFacultyRepository
{
    Task<FacultyEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<FacultyEntity?> GetByEmployeeIdAsync(string employeeId, Guid tenantId, CancellationToken ct = default);
    Task<FacultyEntity?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<List<FacultyEntity>> GetByDepartmentAsync(Guid departmentId, Guid tenantId, CancellationToken ct = default);
    Task<List<FacultyEntity>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> GetPhdCountAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(FacultyEntity faculty, CancellationToken ct = default);
    Task UpdateAsync(FacultyEntity faculty, CancellationToken ct = default);
}
