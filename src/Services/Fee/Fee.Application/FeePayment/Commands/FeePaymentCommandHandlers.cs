using MediatR;
using Fee.Application.Interfaces;
using Fee.Domain.Enums;
using FeePaymentEntity = Fee.Domain.Entities.FeePayment;
namespace Fee.Application.FeePayment.Commands;
public sealed class CreateFeePaymentCommandHandler : IRequestHandler<CreateFeePaymentCommand, Guid>
{
    private readonly IFeePaymentRepository _repository;
    public CreateFeePaymentCommandHandler(IFeePaymentRepository repository) => _repository = repository;
    public async Task<Guid> Handle(CreateFeePaymentCommand cmd, CancellationToken ct)
    {
        var mode = Enum.Parse<PaymentMode>(cmd.PaymentMode, true);
        var payment = FeePaymentEntity.Create(cmd.TenantId, cmd.StudentId, cmd.FeeStructureId, cmd.AmountPaid, mode, cmd.TransactionId, cmd.Gateway);
        await _repository.AddAsync(payment, ct);
        return payment.Id;
    }
}
public sealed class MarkPaymentSuccessCommandHandler : IRequestHandler<MarkPaymentSuccessCommand>
{
    private readonly IFeePaymentRepository _repository;
    public MarkPaymentSuccessCommandHandler(IFeePaymentRepository repository) => _repository = repository;
    public async Task Handle(MarkPaymentSuccessCommand cmd, CancellationToken ct)
    {
        var payment = await _repository.GetByIdAsync(cmd.PaymentId, cmd.TenantId, ct) ?? throw new Exception("Payment not found.");
        payment.MarkSuccess();
        await _repository.UpdateAsync(payment, ct);
    }
}
public sealed class MarkPaymentFailedCommandHandler : IRequestHandler<MarkPaymentFailedCommand>
{
    private readonly IFeePaymentRepository _repository;
    public MarkPaymentFailedCommandHandler(IFeePaymentRepository repository) => _repository = repository;
    public async Task Handle(MarkPaymentFailedCommand cmd, CancellationToken ct)
    {
        var payment = await _repository.GetByIdAsync(cmd.PaymentId, cmd.TenantId, ct) ?? throw new Exception("Payment not found.");
        payment.MarkFailed();
        await _repository.UpdateAsync(payment, ct);
    }
}
public sealed class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand>
{
    private readonly IFeePaymentRepository _repository;
    public RefundPaymentCommandHandler(IFeePaymentRepository repository) => _repository = repository;
    public async Task Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await _repository.GetByIdAsync(cmd.PaymentId, cmd.TenantId, ct) ?? throw new Exception("Payment not found.");
        payment.Refund();
        await _repository.UpdateAsync(payment, ct);
    }
}
