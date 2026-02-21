using Fee.Domain.Entities;
using Fee.Domain.Enums;
using Fee.Domain.Events;
using Fee.Domain.Exceptions;
using FluentAssertions;

namespace Fee.Tests.Domain;

// ─────────────────────────────────────────────────────────────
// FeeStructure — 10 tests
// ─────────────────────────────────────────────────────────────
public class FeeStructureTests
{
    static readonly Guid _tenant    = Guid.NewGuid();
    static readonly Guid _programme = Guid.NewGuid();
    static readonly DateTime _due   = DateTime.UtcNow.AddDays(30);

    static FeeStructure Valid(decimal? hostel = null, decimal? mess = null) =>
        FeeStructure.Create(_tenant, _programme, "2025-26", 3, 50000, 2000, 5000, 1000, _due, hostel, mess);

    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var s = Valid();
        s.TuitionFee.Should().Be(50000);
        s.TotalFee.Should().Be(58000);
        s.HostelFee.Should().BeNull();
        s.AcademicYear.Should().Be("2025-26");
    }

    [Fact]
    public void Create_WithHostelAndMess_TotalIncludesBoth()
    {
        var s = Valid(hostel: 20000, mess: 10000);
        s.TotalFee.Should().Be(88000);
        s.HostelFee.Should().Be(20000);
        s.MessFee.Should().Be(10000);
    }

    [Fact]
    public void Create_EmptyAcademicYear_Throws()
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "  ", 3, 50000, 2000, 5000, 1000, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidSemester_Throws(int sem)
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "2025-26", sem, 50000, 2000, 5000, 1000, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_SEMESTER");
    }

    [Fact]
    public void Create_NegativeTuitionFee_Throws()
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "2025-26", 3, -1, 2000, 5000, 1000, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_FEE");
    }

    [Fact]
    public void Create_NegativeExamFee_Throws()
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "2025-26", 3, 50000, -1, 5000, 1000, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_FEE");
    }

    [Fact]
    public void Create_NegativeDevelopmentFee_Throws()
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "2025-26", 3, 50000, 2000, -1, 1000, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_FEE");
    }

    [Fact]
    public void Create_NegativeMedicalFee_Throws()
    {
        var act = () => FeeStructure.Create(_tenant, _programme, "2025-26", 3, 50000, 2000, 5000, -1, _due);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_FEE");
    }

    [Fact]
    public void Create_DoesNotRaiseDomainEvents()
    {
        var s = Valid();
        s.DomainEvents.Should().BeEmpty();
    }
}

// ─────────────────────────────────────────────────────────────
// FeePayment — 12 tests
// ─────────────────────────────────────────────────────────────
public class FeePaymentTests
{
    static readonly Guid _tenant      = Guid.NewGuid();
    static readonly Guid _student     = Guid.NewGuid();
    static readonly Guid _feeStructId = Guid.NewGuid();

    static FeePayment Valid() =>
        FeePayment.Create(_tenant, _student, _feeStructId, 58000, PaymentMode.Online, "TXN123", "Razorpay");

    [Fact]
    public void Create_ValidArgs_StatusIsPending()
    {
        var p = Valid();
        p.Status.Should().Be(PaymentStatus.Pending);
        p.AmountPaid.Should().Be(58000);
        p.PaymentMode.Should().Be(PaymentMode.Online);
        p.PaidAt.Should().BeNull();
        p.ReceiptNumber.Should().StartWith("RCP-");
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => FeePayment.Create(_tenant, _student, _feeStructId, 0, PaymentMode.Cash);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => FeePayment.Create(_tenant, _student, _feeStructId, -100, PaymentMode.Cash);
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Create_NoDomainEventsOnCreate()
    {
        var p = Valid();
        p.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkSuccess_PendingPayment_SetsSuccessAndPaidAt()
    {
        var p = Valid();
        p.MarkSuccess();
        p.Status.Should().Be(PaymentStatus.Success);
        p.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkSuccess_RaisesFeePaymentReceivedEvent()
    {
        var p = Valid();
        p.MarkSuccess();
        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<FeePaymentReceivedEvent>();
    }

    [Fact]
    public void MarkSuccess_NotPending_Throws()
    {
        var p = Valid();
        p.MarkFailed();
        var act = () => p.MarkSuccess();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void MarkFailed_PendingPayment_SetsFailed()
    {
        var p = Valid();
        p.MarkFailed();
        p.Status.Should().Be(PaymentStatus.Failed);
        p.PaidAt.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_NotPending_Throws()
    {
        var p = Valid();
        p.MarkSuccess();
        var act = () => p.MarkFailed();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Refund_SuccessPayment_SetsRefunded()
    {
        var p = Valid();
        p.MarkSuccess();
        p.Refund();
        p.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Refund_PendingPayment_Throws()
    {
        var p = Valid();
        var act = () => p.Refund();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Refund_FailedPayment_Throws()
    {
        var p = Valid();
        p.MarkFailed();
        var act = () => p.Refund();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }
}

// ─────────────────────────────────────────────────────────────
// Scholarship — 9 tests
// ─────────────────────────────────────────────────────────────
public class ScholarshipTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();

    static Scholarship Valid() =>
        Scholarship.Create(_tenant, _student, "Merit Scholarship", 25000, "2025-26");

    [Fact]
    public void Create_ValidArgs_IsActiveAndRaisesEvent()
    {
        var s = Valid();
        s.IsActive.Should().BeTrue();
        s.Name.Should().Be("Merit Scholarship");
        s.Amount.Should().Be(25000);
        s.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ScholarshipGrantedEvent>();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => Scholarship.Create(_tenant, _student, "  ", 25000, "2025-26");
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_NAME");
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => Scholarship.Create(_tenant, _student, "Merit", 0, "2025-26");
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => Scholarship.Create(_tenant, _student, "Merit", -1, "2025-26");
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Create_EmptyAcademicYear_Throws()
    {
        var act = () => Scholarship.Create(_tenant, _student, "Merit", 25000, "  ");
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Fact]
    public void Deactivate_ActiveScholarship_SetsInactive()
    {
        var s = Valid();
        s.Deactivate();
        s.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var s = Valid();
        s.Deactivate();
        var act = () => s.Deactivate();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("ALREADY_INACTIVE");
    }

    [Fact]
    public void Activate_InactiveScholarship_SetsActive()
    {
        var s = Valid();
        s.Deactivate();
        s.Activate();
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var s = Valid();
        var act = () => s.Activate();
        act.Should().Throw<FeeDomainException>().Which.Code.Should().Be("ALREADY_ACTIVE");
    }
}
