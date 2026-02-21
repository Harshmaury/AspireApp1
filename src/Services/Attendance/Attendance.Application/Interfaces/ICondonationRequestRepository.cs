using Attendance.Domain.Entities;
namespace Attendance.Application.Interfaces;
public interface ICondonationRequestRepository
{
    Task<CondonationRequest?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<CondonationRequest>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task<List<CondonationRequest>> GetPendingAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(CondonationRequest request, CancellationToken ct = default);
    Task UpdateAsync(CondonationRequest request, CancellationToken ct = default);
}
