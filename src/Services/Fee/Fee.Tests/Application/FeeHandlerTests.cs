using Fee.Application.FeePayment.Commands;
using Fee.Application.FeeStructure.Commands;
using Fee.Application.Scholarship.Commands;
using Fee.Application.Interfaces;
using Fee.Domain.Entities;
using Fee.Domain.Enums;
using Fee.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Fee.Tests.Application;

// ─────────────────────────────────────────────────────────────
// CreateFeeStructureCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class CreateFeeStructureCommandHandlerTests
{
    static readonly Guid _tenant    = Guid.NewGuid();
    static readonly Guid _programme = Guid.NewGuid();

    static CreateFeeStructureCommand ValidCmd() => new(
        _tenant, _programme, "2025-26", 3, 50000, 2000, 5000, 1000,
        DateTime.UtcNow.AddDays(30));

    [Fact]
    public async Task Handle_ValidCommand_AddsStructureAndReturnsId()
    {
        var repo    = new Mock<IFeeStructureRepository>();
        var handler = new CreateFeeStructureCommandHandler(repo.Object);

        var id = await handler.Handle(ValidCmd(), CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<FeeStructure>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NegativeTuition_ThrowsDomainException()
    {
        var repo    = new Mock<IFeeStructureRepository>();
        var cmd     = new CreateFeeStructureCommand(_tenant, _programme, "2025-26", 3, -1, 2000, 5000, 1000, DateTime.UtcNow.AddDays(30));
        var handler = new CreateFeeStructureCommandHandler(repo.Object);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FeeDomainException>()
            .Where(e => e.Code == "INVALID_FEE");
    }
}

// ─────────────────────────────────────────────────────────────
// CreateFeePaymentCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class CreateFeePaymentCommandHandlerTests
{
    static readonly Guid _tenant      = Guid.NewGuid();
    static readonly Guid _student     = Guid.NewGuid();
    static readonly Guid _feeStructId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_AddsPaymentAndReturnsId()
    {
        var repo    = new Mock<IFeePaymentRepository>();
        var cmd     = new CreateFeePaymentCommand(_tenant, _student, _feeStructId, 58000, "Online", "TXN123", "Razorpay");
        var handler = new CreateFeePaymentCommandHandler(repo.Object);

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<FeePayment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPaymentMode_Throws()
    {
        var repo    = new Mock<IFeePaymentRepository>();
        var cmd     = new CreateFeePaymentCommand(_tenant, _student, _feeStructId, 58000, "Crypto");
        var handler = new CreateFeePaymentCommandHandler(repo.Object);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

// ─────────────────────────────────────────────────────────────
// MarkPaymentSuccessCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class MarkPaymentSuccessCommandHandlerTests
{
    static readonly Guid _tenant    = Guid.NewGuid();
    static readonly Guid _paymentId = Guid.NewGuid();

    static FeePayment PendingPayment() =>
        FeePayment.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), 58000, PaymentMode.Online);

    [Fact]
    public async Task Handle_PendingPayment_MarksSuccessAndUpdates()
    {
        var repo    = new Mock<IFeePaymentRepository>();
        var payment = PendingPayment();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new MarkPaymentSuccessCommandHandler(repo.Object);
        await handler.Handle(new MarkPaymentSuccessCommand(_tenant, _paymentId), CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Success);
        repo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IFeePaymentRepository>();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeePayment?)null);

        var handler = new MarkPaymentSuccessCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new MarkPaymentSuccessCommand(_tenant, _paymentId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────
// MarkPaymentFailedCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class MarkPaymentFailedCommandHandlerTests
{
    static readonly Guid _tenant    = Guid.NewGuid();
    static readonly Guid _paymentId = Guid.NewGuid();

    static FeePayment PendingPayment() =>
        FeePayment.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), 58000, PaymentMode.Cash);

    [Fact]
    public async Task Handle_PendingPayment_MarksFailedAndUpdates()
    {
        var repo    = new Mock<IFeePaymentRepository>();
        var payment = PendingPayment();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new MarkPaymentFailedCommandHandler(repo.Object);
        await handler.Handle(new MarkPaymentFailedCommand(_tenant, _paymentId), CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Failed);
        repo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IFeePaymentRepository>();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeePayment?)null);

        var handler = new MarkPaymentFailedCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new MarkPaymentFailedCommand(_tenant, _paymentId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────
// RefundPaymentCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class RefundPaymentCommandHandlerTests
{
    static readonly Guid _tenant    = Guid.NewGuid();
    static readonly Guid _paymentId = Guid.NewGuid();

    static FeePayment SuccessPayment()
    {
        var p = FeePayment.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), 58000, PaymentMode.DD);
        p.MarkSuccess();
        return p;
    }

    [Fact]
    public async Task Handle_SuccessPayment_RefundsAndUpdates()
    {
        var repo    = new Mock<IFeePaymentRepository>();
        var payment = SuccessPayment();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = new RefundPaymentCommandHandler(repo.Object);
        await handler.Handle(new RefundPaymentCommand(_tenant, _paymentId), CancellationToken.None);

        payment.Status.Should().Be(PaymentStatus.Refunded);
        repo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IFeePaymentRepository>();
        repo.Setup(r => r.GetByIdAsync(_paymentId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeePayment?)null);

        var handler = new RefundPaymentCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new RefundPaymentCommand(_tenant, _paymentId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────
// CreateScholarshipCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class CreateScholarshipCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_AddsScholarshipAndReturnsId()
    {
        var repo    = new Mock<IScholarshipRepository>();
        var cmd     = new CreateScholarshipCommand(_tenant, _student, "Merit Scholarship", 25000, "2025-26");
        var handler = new CreateScholarshipCommandHandler(repo.Object);

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<Scholarship>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsDomainException()
    {
        var repo    = new Mock<IScholarshipRepository>();
        var cmd     = new CreateScholarshipCommand(_tenant, _student, "  ", 25000, "2025-26");
        var handler = new CreateScholarshipCommandHandler(repo.Object);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FeeDomainException>()
            .Where(e => e.Code == "INVALID_NAME");
    }
}
