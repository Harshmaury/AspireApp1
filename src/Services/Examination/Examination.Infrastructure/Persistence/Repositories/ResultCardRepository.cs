using Microsoft.EntityFrameworkCore;
using Examination.Application.Interfaces;
using Examination.Infrastructure.Persistence;
using Examination.Domain.Entities;
namespace Examination.Infrastructure.Persistence.Repositories;
public sealed class ResultCardRepository : IResultCardRepository
{
    private readonly ExaminationDbContext _context;
    public ResultCardRepository(ExaminationDbContext context) => _context = context;
    public async Task<ResultCard?> GetByStudentSemesterAsync(Guid studentId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default) =>
        await _context.ResultCards.FirstOrDefaultAsync(e => e.StudentId == studentId && e.AcademicYear == academicYear && e.Semester == semester && e.TenantId == tenantId, ct);
    public async Task<List<ResultCard>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        await _context.ResultCards.Where(e => e.StudentId == studentId && e.TenantId == tenantId).ToListAsync(ct);
    public async Task AddAsync(ResultCard resultCard, CancellationToken ct = default) =>
        await _context.ResultCards.AddAsync(resultCard, ct);
    public async Task UpdateAsync(ResultCard resultCard, CancellationToken ct = default) =>
        await Task.FromResult(_context.ResultCards.Update(resultCard));
}
