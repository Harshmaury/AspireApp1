using Microsoft.EntityFrameworkCore;
using Examination.Application.Interfaces;
using Examination.Infrastructure.Persistence;
using MarksEntryEntity = Examination.Domain.Entities.MarksEntry;
namespace Examination.Infrastructure.Persistence.Repositories;
public sealed class MarksEntryRepository : IMarksEntryRepository
{
    private readonly ExaminationDbContext _context;
    public MarksEntryRepository(ExaminationDbContext context) => _context = context;
    public async Task<MarksEntryEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.MarksEntries.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
    public async Task<List<MarksEntryEntity>> GetByExamScheduleAsync(Guid examScheduleId, Guid tenantId, CancellationToken ct = default) =>
        await _context.MarksEntries.Where(e => e.ExamScheduleId == examScheduleId && e.TenantId == tenantId).ToListAsync(ct);
    public async Task<List<MarksEntryEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        await _context.MarksEntries.Where(e => e.StudentId == studentId && e.TenantId == tenantId).ToListAsync(ct);
    public async Task AddAsync(MarksEntryEntity entry, CancellationToken ct = default)
    {
        await _context.MarksEntries.AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct);
    }
    public async Task UpdateAsync(MarksEntryEntity entry, CancellationToken ct = default)
    {
        _context.MarksEntries.Update(entry);
        await _context.SaveChangesAsync(ct);
    }
}
