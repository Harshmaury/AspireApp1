using Attendance.Domain.Entities;
using Attendance.Domain.Enums;
using Attendance.Domain.Events;
using Attendance.Domain.Exceptions;
using FluentAssertions;

namespace Attendance.Tests.Domain;

public class AttendanceRecordTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();
    static readonly Guid _marker  = Guid.NewGuid();
    static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    static AttendanceRecord Valid() =>
        AttendanceRecord.Create(_tenant, _student, _course, "2025-26", 3, _today, ClassType.Lecture, true, _marker);

    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var r = Valid();
        r.TenantId.Should().Be(_tenant);
        r.StudentId.Should().Be(_student);
        r.AcademicYear.Should().Be("2025-26");
        r.Semester.Should().Be(3);
        r.IsPresent.Should().BeTrue();
        r.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyAcademicYear_Throws()
    {
        var act = () => AttendanceRecord.Create(_tenant, _student, _course, "  ", 3, _today, ClassType.Lecture, true, _marker);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidSemester_Throws(int sem)
    {
        var act = () => AttendanceRecord.Create(_tenant, _student, _course, "2025-26", sem, _today, ClassType.Lecture, true, _marker);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_SEMESTER");
    }

    [Fact]
    public void Create_FutureDate_Throws()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var act = () => AttendanceRecord.Create(_tenant, _student, _course, "2025-26", 3, future, ClassType.Lecture, true, _marker);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_DATE");
    }

    [Fact]
    public void Create_TooOldDate_Throws()
    {
        var old = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8));
        var act = () => AttendanceRecord.Create(_tenant, _student, _course, "2025-26", 3, old, ClassType.Lecture, true, _marker);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("BACKDATING_NOT_ALLOWED");
    }

    [Fact]
    public void Create_RaisesAttendanceMarkedEvent()
    {
        var r = Valid();
        r.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AttendanceMarkedEvent>();
    }

    [Fact]
    public void Lock_UnlockedRecord_SetsIsLockedTrue()
    {
        var r = Valid();
        r.Lock();
        r.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Lock_AlreadyLocked_Throws()
    {
        var r = Valid();
        r.Lock();
        var act = () => r.Lock();
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("ALREADY_LOCKED");
    }

    [Fact]
    public void Correct_UpdatesIsPresent()
    {
        var r = Valid();
        r.Correct(false, Guid.NewGuid());
        r.IsPresent.Should().BeFalse();
    }

    [Fact]
    public void Correct_LockedRecord_Throws()
    {
        var r = Valid();
        r.Lock();
        var act = () => r.Correct(false, Guid.NewGuid());
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("RECORD_LOCKED");
    }
}

public class AttendanceSummaryTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();

    static AttendanceSummary Valid() =>
        AttendanceSummary.Create(_tenant, _student, _course, "2025-26", 3);

    [Fact]
    public void Create_ValidArgs_SetsDefaults()
    {
        var s = Valid();
        s.TotalClasses.Should().Be(0);
        s.AttendedClasses.Should().Be(0);
        s.Percentage.Should().Be(100m);
        s.IsShortage.Should().BeFalse();
        s.IsWarning.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyYear_Throws()
    {
        var act = () => AttendanceSummary.Create(_tenant, _student, _course, "", 3);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidSemester_Throws(int sem)
    {
        var act = () => AttendanceSummary.Create(_tenant, _student, _course, "2025-26", sem);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_SEMESTER");
    }

    [Fact]
    public void Refresh_ZeroClasses_PercentageIs100()
    {
        var s = Valid();
        s.Refresh(0, 0);
        s.Percentage.Should().Be(100m);
        s.IsShortage.Should().BeFalse();
        s.IsWarning.Should().BeFalse();
    }

    [Fact]
    public void Refresh_CalculatesPercentageCorrectly()
    {
        var s = Valid();
        s.Refresh(10, 8);
        s.Percentage.Should().Be(80.00m);
        s.IsWarning.Should().BeFalse();
        s.IsShortage.Should().BeFalse();
    }

    [Fact]
    public void Refresh_WarningBand_SetsIsWarning()
    {
        var s = Valid();
        s.Refresh(20, 15);
        s.IsWarning.Should().BeTrue();
        s.IsShortage.Should().BeFalse();
    }

    [Fact]
    public void Refresh_BelowShortageThreshold_SetsIsShortage()
    {
        var s = Valid();
        s.Refresh(100, 74);
        s.IsShortage.Should().BeTrue();
        s.IsWarning.Should().BeFalse();
    }

    [Fact]
    public void Refresh_FirstShortage_RaisesAttendanceShortageFlaggedEvent()
    {
        var s = Valid();
        s.Refresh(100, 74);
        s.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AttendanceShortageFlaggedEvent>();
    }

    [Fact]
    public void Refresh_AlreadyShortage_DoesNotRaiseEventAgain()
    {
        var s = Valid();
        s.Refresh(100, 74);
        s.ClearDomainEvents();
        s.Refresh(100, 70);
        s.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Refresh_NegativeTotal_Throws()
    {
        var s = Valid();
        var act = () => s.Refresh(-1, 0);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_TOTAL");
    }

    [Fact]
    public void Refresh_AttendedExceedsTotal_Throws()
    {
        var s = Valid();
        var act = () => s.Refresh(5, 6);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_ATTENDED");
    }

    [Theory]
    [InlineData(100, 75, true)]
    [InlineData(100, 74, false)]
    public void IsEligibleForExam_ReturnsCorrectly(int total, int attended, bool expected)
    {
        var s = Valid();
        s.Refresh(total, attended);
        s.IsEligibleForExam().Should().Be(expected);
    }
}

public class CondonationRequestTests
{
    static readonly Guid _tenant     = Guid.NewGuid();
    static readonly Guid _student    = Guid.NewGuid();
    static readonly Guid _course     = Guid.NewGuid();
    static readonly Guid _reviewedBy = Guid.NewGuid();

    static CondonationRequest Valid() =>
        CondonationRequest.Create(_tenant, _student, _course, "Medical leave for hospitalization");

    [Fact]
    public void Create_ValidArgs_StatusIsPending()
    {
        var r = Valid();
        r.Status.Should().Be(CondonationStatus.Pending);
        r.Reason.Should().Be("Medical leave for hospitalization");
        r.ReviewedBy.Should().BeNull();
        r.ReviewedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        var act = () => CondonationRequest.Create(_tenant, _student, _course, "  ");
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_REASON");
    }

    [Fact]
    public void Create_ReasonTooLong_Throws()
    {
        var act = () => CondonationRequest.Create(_tenant, _student, _course, new string('x', 1001));
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("REASON_TOO_LONG");
    }

    [Fact]
    public void Approve_PendingRequest_SetsApprovedAndNote()
    {
        var r = Valid();
        r.Approve(_reviewedBy, "Supported by medical certificate");
        r.Status.Should().Be(CondonationStatus.Approved);
        r.ReviewedBy.Should().Be(_reviewedBy);
        r.ReviewNote.Should().Be("Supported by medical certificate");
        r.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_RaisesCondonationApprovedEvent()
    {
        var r = Valid();
        r.Approve(_reviewedBy);
        r.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CondonationApprovedEvent>();
    }

    [Fact]
    public void Approve_NotPending_Throws()
    {
        var r = Valid();
        r.Approve(_reviewedBy);
        var act = () => r.Approve(_reviewedBy);
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Reject_PendingRequest_SetsRejected()
    {
        var r = Valid();
        r.Reject(_reviewedBy, "Documents insufficient");
        r.Status.Should().Be(CondonationStatus.Rejected);
        r.ReviewNote.Should().Be("Documents insufficient");
        r.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_NotPending_Throws()
    {
        var r = Valid();
        r.Reject(_reviewedBy, "First rejection");
        var act = () => r.Reject(_reviewedBy, "Second rejection");
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Reject_EmptyNote_Throws()
    {
        var r = Valid();
        var act = () => r.Reject(_reviewedBy, "  ");
        act.Should().Throw<AttendanceDomainException>().Which.Code.Should().Be("REVIEW_NOTE_REQUIRED");
    }
}
