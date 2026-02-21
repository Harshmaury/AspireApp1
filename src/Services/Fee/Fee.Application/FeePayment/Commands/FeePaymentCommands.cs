using MediatR;
namespace Fee.Application.FeePayment.Commands;
public sealed record CreateFeePaymentCommand(
    Guid TenantId,
    Guid StudentId,
    Guid FeeStructureId,
    decimal AmountPaid,
    string PaymentMode,
    string? TransactionId = null,
    string? Gateway = null) : IRequest<Guid>;
public sealed record MarkPaymentSuccessCommand(Guid TenantId, Guid PaymentId) : IRequest;
public sealed record MarkPaymentFailedCommand(Guid TenantId, Guid PaymentId) : IRequest;
public sealed record RefundPaymentCommand(Guid TenantId, Guid PaymentId) : IRequest;
