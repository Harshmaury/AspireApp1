using Fee.Domain.Common;
using Fee.Domain.Enums;
using Fee.Domain.Events;
using Fee.Domain.Exceptions;
namespace Fee.Domain.Entities;
public sealed class FeePayment : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid FeeStructureId { get; private set; }
    public decimal AmountPaid { get; private set; }
    public PaymentMode PaymentMode { get; private set; }
    public string? TransactionId { get; private set; }
    public string? Gateway { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string ReceiptNumber { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private FeePayment() { }
    public static FeePayment Create(Guid tenantId, Guid studentId, Guid feeStructureId, decimal amountPaid, PaymentMode paymentMode, string? transactionId = null, string? gateway = null)
    {
        if (amountPaid <= 0) throw new FeeDomainException("INVALID_AMOUNT", "Amount paid must be positive.");
        var payment = new FeePayment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            FeeStructureId = feeStructureId,
            AmountPaid = amountPaid,
            PaymentMode = paymentMode,
            TransactionId = transactionId,
            Gateway = gateway,
            Status = PaymentStatus.Pending,
            ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            CreatedAt = DateTime.UtcNow
        };
        return payment;
    }
    public void MarkSuccess()
    {
        if (Status != PaymentStatus.Pending) throw new FeeDomainException("INVALID_STATUS", "Only pending payments can be marked as success.");
        Status = PaymentStatus.Success;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new FeePaymentReceivedEvent(Id, StudentId, TenantId, AmountPaid));
    }
    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending) throw new FeeDomainException("INVALID_STATUS", "Only pending payments can be marked as failed.");
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Refund()
    {
        if (Status != PaymentStatus.Success) throw new FeeDomainException("INVALID_STATUS", "Only successful payments can be refunded.");
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
