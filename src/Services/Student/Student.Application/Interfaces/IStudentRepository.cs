using Student.Domain.Entities;

namespace Student.Application.Interfaces;

public interface IStudentRepository
{
    Task<StudentAggregate?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<StudentAggregate?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(StudentAggregate student, CancellationToken ct = default);
    Task UpdateAsync(StudentAggregate student, CancellationToken ct = default);
}
