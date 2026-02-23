using Microsoft.EntityFrameworkCore;
using Fee.Application.Interfaces;
using Fee.Infrastructure.Persistence;
using FeePaymentEntity = Fee.Domain.Entities.FeePayment;
namespace Fee.Infrastructure.Persistence.Repositories;
public sealed class FeePaymentRepository : IFeePaymentRepository
{
    private readonly FeeDbContext _context;
    public FeePaymentRepository(FeeDbContext context) => _context = context;
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
        _context.FeePayments.Update(payment);
        await _context.SaveChangesAsync(ct);
    }
}
