using Examination.Application.Interfaces;
using Examination.Domain.Entities;
using Examination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;

namespace Examination.Infrastructure.Persistence.Repositories;

internal sealed class ResultCardRepository : IResultCardRepository
{
    private readonly ExaminationDbContext _context;
    public ResultCardRepository(ExaminationDbContext context, ITenantContext? tenant = null) => _context = context;

    public async Task<ResultCard?> GetByStudentSemesterAsync(Guid studentId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default) =>
        await _context.ResultCards.FirstOrDefaultAsync(
            e => e.StudentId == studentId && e.AcademicYear == academicYear &&
                 e.Semester == semester && e.TenantId == tenantId, ct);

    public async Task<List<ResultCard>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        await _context.ResultCards.Where(e => e.StudentId == studentId && e.TenantId == tenantId).ToListAsync(ct);

    public async Task AddAsync(ResultCard resultCard, CancellationToken ct = default) =>
        await _context.ResultCards.AddAsync(resultCard, ct);

    public async Task UpdateAsync(ResultCard resultCard, CancellationToken ct = default)
    {
        if (_context.Entry(resultCard).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached ResultCard (Id={resultCard.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _context.SaveChangesAsync(ct);
    }
}

