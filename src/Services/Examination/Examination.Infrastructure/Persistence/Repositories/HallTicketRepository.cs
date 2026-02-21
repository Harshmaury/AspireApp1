using Microsoft.EntityFrameworkCore;
using Examination.Application.Interfaces;
using Examination.Infrastructure.Persistence;
using Examination.Domain.Entities;
namespace Examination.Infrastructure.Persistence.Repositories;
public sealed class HallTicketRepository : IHallTicketRepository
{
    private readonly ExaminationDbContext _context;
    public HallTicketRepository(ExaminationDbContext context) => _context = context;
    public async Task<HallTicket?> GetByStudentExamAsync(Guid studentId, Guid examScheduleId, Guid tenantId, CancellationToken ct = default) =>
        await _context.HallTickets.FirstOrDefaultAsync(e => e.StudentId == studentId && e.ExamScheduleId == examScheduleId && e.TenantId == tenantId, ct);
    public async Task AddAsync(HallTicket hallTicket, CancellationToken ct = default) =>
        await _context.HallTickets.AddAsync(hallTicket, ct);
}
