using Fee.Application.Interfaces;
using Fee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Tenancy;
using FeePaymentEntity = Fee.Domain.Entities.FeePayment;

namespace Fee.Infrastructure.Persistence.Repositories;

internal sealed class FeePaymentRepository : IFeePaymentRepository
{
    private readonly FeeDbContext _context;
    public FeePaymentRepository(FeeDbContext context, ITenantContext? tenant = null) => _context = context;

    public async Task<FeePaymentEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeePayments.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);

    public async Task<List<FeePaymentEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default) =>
        await _context.FeePayments.Where(e => e.StudentId == studentId && e.TenantId == tenantId).ToListAsync(ct);

    public async Task<List<FeePaymentEntity>> GetDefaultersAsync(Guid tenantId, string academicYear, CancellationToken ct = default) =>
        await _context.FeePayments.Where(e => e.TenantId == tenantId && e.Status == Fee.Domain.Enums.PaymentStatus.Pending).ToListAsync(ct);

    public async Task AddAsync(FeePaymentEntity payment, CancellationToken ct = default)
    {
        await _context.FeePayments.AddAsync(payment, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FeePaymentEntity payment, CancellationToken ct = default)
    {
        if (_context.Entry(payment).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"UpdateAsync received a detached FeePayment (Id={payment.Id}). " +
                "Fetch the entity via the repository in the same handler scope before mutating it.");
        await _context.SaveChangesAsync(ct);
    }
}

