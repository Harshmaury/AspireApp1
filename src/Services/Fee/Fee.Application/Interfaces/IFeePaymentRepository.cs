using FeePaymentEntity = Fee.Domain.Entities.FeePayment;
namespace Fee.Application.Interfaces;
public interface IFeePaymentRepository
{
    Task<FeePaymentEntity?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<FeePaymentEntity>> GetByStudentAsync(Guid studentId, Guid tenantId, CancellationToken ct = default);
    Task<List<FeePaymentEntity>> GetDefaultersAsync(Guid tenantId, string academicYear, CancellationToken ct = default);
    Task AddAsync(FeePaymentEntity payment, CancellationToken ct = default);
    Task UpdateAsync(FeePaymentEntity payment, CancellationToken ct = default);
}
