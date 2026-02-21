using Microsoft.EntityFrameworkCore;
using Fee.Application.Interfaces;
using Fee.Infrastructure.Persistence;
using FeeStructureEntity = Fee.Domain.Entities.FeeStructure;
namespace Fee.Infrastructure.Persistence.Repositories;
public sealed class FeeStructureRepository : IFeeStructureRepository
{
    private readonly FeeDbContext _context;
    public FeeStructureRepository(FeeDbContext context) => _context = context;
    public async Task<FeeStructureEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
    public async Task<FeeStructureEntity?> GetByProgrammeSemesterAsync(Guid programmeId, string academicYear, int semester, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.FirstOrDefaultAsync(e => e.ProgrammeId == programmeId && e.AcademicYear == academicYear && e.Semester == semester && e.TenantId == tenantId, ct);
    public async Task<List<FeeStructureEntity>> GetByProgrammeAsync(Guid programmeId, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeeStructures.Where(e => e.ProgrammeId == programmeId && e.TenantId == tenantId).ToListAsync(ct);
    public async Task AddAsync(FeeStructureEntity feeStructure, CancellationToken ct = default) =>
        await _context.FeeStructures.AddAsync(feeStructure, ct);
    public async Task UpdateAsync(FeeStructureEntity feeStructure, CancellationToken ct = default) =>
        await Task.FromResult(_context.FeeStructures.Update(feeStructure));
}
