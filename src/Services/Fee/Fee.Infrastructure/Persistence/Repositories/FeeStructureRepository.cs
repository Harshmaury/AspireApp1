using Fee.Application.Interfaces;
using Fee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using FeeStructureEntity = Fee.Domain.Entities.FeeStructure;

namespace Fee.Infrastructure.Persistence.Repositories;

internal sealed class FeeStructureRepository : IFeeStructureRepository
{
    private readonly FeeDbContext _context;
    public FeeStructureRepository(FeeDbContext context, ITenantContext? tenant = null) => _context = context;

    public async Task<FeeStructureEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

    public async Task<FeeStructureEntity?> GetByProgrammeSemesterAsync(Guid programmeId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.FirstOrDefaultAsync(
            e => e.ProgrammeId == programmeId && e.AcademicYear == academicYear &&
                 e.Semester == semester && e.TenantId == tenantId, ct);

    public async Task<List<FeeStructureEntity>> GetByProgrammeAsync(Guid programmeId, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.Where(e => e.ProgrammeId == programmeId && e.TenantId == tenantId).ToListAsync(ct);

    public async Task AddAsync(FeeStructureEntity feeStructure, CancellationToken ct = default) =>
        await _context.FeeStructures.AddAsync(feeStructure, ct);

    public async Task UpdateAsync(FeeStructureEntity feeStructure, CancellationToken ct = default)
    {
        if (_context.Entry(feeStructure).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached FeeStructure (Id={feeStructure.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _context.SaveChangesAsync(ct);
    }
}

