using Student.Domain.Entities;

namespace Student.Application.Interfaces;

public interface IStudentRepository
{
    // WRITE PATH - tracked. Use in command handlers only.
    Task<StudentAggregate?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<StudentAggregate?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    // READ-ONLY PATH - untracked. Use in query handlers only.
    Task<StudentAggregate?> GetByIdReadOnlyAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<(List<StudentAggregate> Items, int TotalCount)> GetAllAsync(
        Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(StudentAggregate student, CancellationToken ct = default);
    Task UpdateAsync(StudentAggregate student, CancellationToken ct = default);
}