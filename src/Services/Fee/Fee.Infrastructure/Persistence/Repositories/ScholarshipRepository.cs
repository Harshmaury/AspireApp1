using Fee.Application.Interfaces;
using Fee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using ScholarshipEntity = Fee.Domain.Entities.Scholarship;

namespace Fee.Infrastructure.Persistence.Repositories;

internal sealed class ScholarshipRepository : IScholarshipRepository
{
    private readonly FeeDbContext _context;
    public ScholarshipRepository(FeeDbContext context, ITenantContext? tenant = null) => _context = context;

    public async Task<ScholarshipEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.Scholarships.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

    public async Task<List<ScholarshipEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        await _context.Scholarships.Where(e => e.StudentId == studentId && e.TenantId == tenantId).ToListAsync(ct);

    public async Task AddAsync(ScholarshipEntity scholarship, CancellationToken ct = default) =>
        await _context.Scholarships.AddAsync(scholarship, ct);

    public async Task UpdateAsync(ScholarshipEntity scholarship, CancellationToken ct = default)
    {
        if (_context.Entry(scholarship).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached Scholarship (Id={scholarship.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _context.SaveChangesAsync(ct);
    }
}

