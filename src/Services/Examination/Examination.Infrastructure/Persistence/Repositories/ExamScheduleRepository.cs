using Examination.Application.Interfaces;
using Examination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using ExamScheduleEntity = Examination.Domain.Entities.ExamSchedule;

namespace Examination.Infrastructure.Persistence.Repositories;

internal sealed class ExamScheduleRepository : IExamScheduleRepository
{
    private readonly ExaminationDbContext _context;
    public ExamScheduleRepository(ExaminationDbContext context, ITenantContext? tenant = null) => _context = context;

    public async Task<ExamScheduleEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.ExamSchedules.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

    public async Task<List<ExamScheduleEntity>> GetByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default) =>
        await _context.ExamSchedules.Where(e => e.CourseId == courseId && e.TenantId == tenantId).ToListAsync(ct);

    public async Task AddAsync(ExamScheduleEntity schedule, CancellationToken ct = default)
    {
        await _context.ExamSchedules.AddAsync(schedule, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExamScheduleEntity schedule, CancellationToken ct = default)
    {
        if (_context.Entry(schedule).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached ExamSchedule (Id={schedule.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _context.SaveChangesAsync(ct);
    }
}

