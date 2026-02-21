using Microsoft.EntityFrameworkCore;
using Examination.Application.Interfaces;
using Examination.Infrastructure.Persistence;
using ExamScheduleEntity = Examination.Domain.Entities.ExamSchedule;
namespace Examination.Infrastructure.Persistence.Repositories;
public sealed class ExamScheduleRepository : IExamScheduleRepository
{
    private readonly ExaminationDbContext _context;
    public ExamScheduleRepository(ExaminationDbContext context) => _context = context;
    public async Task<ExamScheduleEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.ExamSchedules.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
    public async Task<List<ExamScheduleEntity>> GetByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default) =>
        await _context.ExamSchedules.Where(e => e.CourseId == courseId && e.TenantId == tenantId).ToListAsync(ct);
    public async Task AddAsync(ExamScheduleEntity schedule, CancellationToken ct = default) =>
        await _context.ExamSchedules.AddAsync(schedule, ct);
    public async Task UpdateAsync(ExamScheduleEntity schedule, CancellationToken ct = default) =>
        await Task.FromResult(_context.ExamSchedules.Update(schedule));
}
