using Examination.Domain.Entities;
namespace Examination.Application.Interfaces;
public interface IHallTicketRepository
{
    Task<HallTicket?> GetByStudentExamAsync(Guid studentId, Guid examScheduleId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(HallTicket hallTicket, CancellationToken ct = default);
}
