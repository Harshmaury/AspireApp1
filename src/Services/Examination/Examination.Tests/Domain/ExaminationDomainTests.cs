using Examination.Domain.Entities;
using Examination.Domain.Enums;
using Examination.Domain.Events;
using Examination.Domain.Exceptions;
using FluentAssertions;

namespace Examination.Tests.Domain;

// ─────────────────────────────────────────────────────────────
// ExamSchedule — 13 tests
// ─────────────────────────────────────────────────────────────
public class ExamScheduleTests
{
    static readonly Guid _tenant = Guid.NewGuid();
    static readonly Guid _course = Guid.NewGuid();
    static readonly DateTime _future = DateTime.UtcNow.AddDays(10);

    static ExamSchedule Valid() =>
        ExamSchedule.Create(_tenant, _course, "2025-26", 3, ExamType.EndSem, _future, 180, "Hall A", 100, 40);

    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var s = Valid();
        s.TenantId.Should().Be(_tenant);
        s.AcademicYear.Should().Be("2025-26");
        s.Status.Should().Be(ExamStatus.Scheduled);
        s.MaxMarks.Should().Be(100);
        s.PassingMarks.Should().Be(40);
    }

    [Fact]
    public void Create_RaisesExamScheduledEvent()
    {
        var s = Valid();
        s.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ExamScheduledEvent>();
    }

    [Fact]
    public void Create_EmptyAcademicYear_Throws()
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "  ", 3, ExamType.EndSem, _future, 180, "Hall A", 100, 40);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidSemester_Throws(int sem)
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "2025-26", sem, ExamType.EndSem, _future, 180, "Hall A", 100, 40);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_SEMESTER");
    }

    [Fact]
    public void Create_ZeroDuration_Throws()
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "2025-26", 3, ExamType.EndSem, _future, 0, "Hall A", 100, 40);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_DURATION");
    }

    [Fact]
    public void Create_EmptyVenue_Throws()
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "2025-26", 3, ExamType.EndSem, _future, 180, "  ", 100, 40);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_VENUE");
    }

    [Fact]
    public void Create_ZeroMaxMarks_Throws()
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "2025-26", 3, ExamType.EndSem, _future, 180, "Hall A", 0, 40);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_MARKS");
    }

    [Fact]
    public void Create_PassingMarksExceedsMax_Throws()
    {
        var act = () => ExamSchedule.Create(_tenant, _course, "2025-26", 3, ExamType.EndSem, _future, 180, "Hall A", 100, 101);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_PASSING_MARKS");
    }

    [Fact]
    public void Start_ScheduledExam_SetsOngoing()
    {
        var s = Valid();
        s.Start();
        s.Status.Should().Be(ExamStatus.Ongoing);
    }

    [Fact]
    public void Start_NotScheduled_Throws()
    {
        var s = Valid();
        s.Start();
        var act = () => s.Start();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Complete_OngoingExam_SetsCompleted()
    {
        var s = Valid();
        s.Start();
        s.Complete();
        s.Status.Should().Be(ExamStatus.Completed);
    }

    [Fact]
    public void Complete_NotOngoing_Throws()
    {
        var s = Valid();
        var act = () => s.Complete();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Cancel_CompletedExam_Throws()
    {
        var s = Valid();
        s.Start();
        s.Complete();
        var act = () => s.Cancel();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }
}

// ─────────────────────────────────────────────────────────────
// HallTicket — 5 tests
// ─────────────────────────────────────────────────────────────
public class HallTicketTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _examId  = Guid.NewGuid();

    [Fact]
    public void Create_EligibleStudent_SetsProperties()
    {
        var h = HallTicket.Create(_tenant, _student, _examId, "R001", "S-12", true);
        h.RollNumber.Should().Be("R001");
        h.SeatNumber.Should().Be("S-12");
        h.IsEligible.Should().BeTrue();
        h.IneligibilityReason.Should().BeNull();
    }

    [Fact]
    public void Create_IneligibleWithReason_SetsReason()
    {
        var h = HallTicket.Create(_tenant, _student, _examId, "R001", "S-12", false, "Attendance shortage");
        h.IsEligible.Should().BeFalse();
        h.IneligibilityReason.Should().Be("Attendance shortage");
    }

    [Fact]
    public void Create_EmptyRollNumber_Throws()
    {
        var act = () => HallTicket.Create(_tenant, _student, _examId, "  ", "S-12", true);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_ROLL");
    }

    [Fact]
    public void Create_EmptySeatNumber_Throws()
    {
        var act = () => HallTicket.Create(_tenant, _student, _examId, "R001", "  ", true);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_SEAT");
    }

    [Fact]
    public void Create_IneligibleWithoutReason_Throws()
    {
        var act = () => HallTicket.Create(_tenant, _student, _examId, "R001", "S-12", false);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INELIGIBILITY_REASON_REQUIRED");
    }
}

// ─────────────────────────────────────────────────────────────
// MarksEntry — 13 tests
// ─────────────────────────────────────────────────────────────
public class MarksEntryTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _exam    = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();
    static readonly Guid _by      = Guid.NewGuid();

    static MarksEntry Valid(decimal marks = 75, int max = 100, bool absent = false) =>
        MarksEntry.Create(_tenant, _student, _exam, _course, marks, max, absent, _by);

    [Fact]
    public void Create_ValidMarks_StatusIsDraft()
    {
        var m = Valid();
        m.Status.Should().Be(MarksStatus.Draft);
        m.IsAbsent.Should().BeFalse();
    }

    [Fact]
    public void Create_NegativeMarks_Throws()
    {
        var act = () => Valid(-1);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_MARKS");
    }

    [Fact]
    public void Create_MarksExceedMax_Throws()
    {
        var act = () => Valid(101);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_MARKS");
    }

    [Fact]
    public void Create_Absent_GradeIsFWithZeroMarks()
    {
        var m = Valid(0, 100, absent: true);
        m.Grade.Should().Be("F");
        m.GradePoint.Should().Be(0);
        m.MarksObtained.Should().Be(0);
    }

    [Theory]
    [InlineData(90, "O",  10)]
    [InlineData(80, "A+",  9)]
    [InlineData(70, "A",   8)]
    [InlineData(60, "B+",  7)]
    [InlineData(50, "B",   6)]
    [InlineData(45, "C",   5)]
    [InlineData(40, "P",   4)]
    [InlineData(30, "F",   0)]
    public void Create_GradeComputedCorrectly(decimal marks, string expectedGrade, decimal expectedGP)
    {
        var m = Valid(marks);
        m.Grade.Should().Be(expectedGrade);
        m.GradePoint.Should().Be(expectedGP);
    }

    [Fact]
    public void Submit_DraftEntry_SetsSubmitted()
    {
        var m = Valid();
        m.Submit();
        m.Status.Should().Be(MarksStatus.Submitted);
    }

    [Fact]
    public void Submit_NotDraft_Throws()
    {
        var m = Valid();
        m.Submit();
        var act = () => m.Submit();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Approve_SubmittedEntry_SetsApproved()
    {
        var m = Valid();
        m.Submit();
        m.Approve(_by);
        m.Status.Should().Be(MarksStatus.Approved);
        m.ApprovedBy.Should().Be(_by);
    }

    [Fact]
    public void Approve_NotSubmitted_Throws()
    {
        var m = Valid();
        var act = () => m.Approve(_by);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void Publish_ApprovedEntry_SetsPublishedAndRaisesEvent()
    {
        var m = Valid();
        m.Submit();
        m.Approve(_by);
        m.Publish();
        m.Status.Should().Be(MarksStatus.Published);
        m.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MarksPublishedEvent>();
    }

    [Fact]
    public void Publish_NotApproved_Throws()
    {
        var m = Valid();
        m.Submit();
        var act = () => m.Publish();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_STATUS");
    }
}

// ─────────────────────────────────────────────────────────────
// ResultCard — 10 tests
// ─────────────────────────────────────────────────────────────
public class ResultCardTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();

    static ResultCard Valid(int earned = 24, int attempted = 24) =>
        ResultCard.Create(_tenant, _student, "2025-26", 3, 8.5m, 8.2m, earned, attempted);

    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        var r = Valid();
        r.SGPA.Should().Be(8.5m);
        r.CGPA.Should().Be(8.2m);
        r.HasBacklog.Should().BeFalse();
        r.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyYear_Throws()
    {
        var act = () => ResultCard.Create(_tenant, _student, "  ", 3, 8.5m, 8.2m, 24, 24);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_YEAR");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidSemester_Throws(int sem)
    {
        var act = () => ResultCard.Create(_tenant, _student, "2025-26", sem, 8.5m, 8.2m, 24, 24);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_SEMESTER");
    }

    [Fact]
    public void Create_SGPAOutOfRange_Throws()
    {
        var act = () => ResultCard.Create(_tenant, _student, "2025-26", 3, 10.1m, 8.2m, 24, 24);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_SGPA");
    }

    [Fact]
    public void Create_CGPAOutOfRange_Throws()
    {
        var act = () => ResultCard.Create(_tenant, _student, "2025-26", 3, 8.5m, -0.1m, 24, 24);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_CGPA");
    }

    [Fact]
    public void Create_CreditsEarnedExceedsAttempted_Throws()
    {
        var act = () => ResultCard.Create(_tenant, _student, "2025-26", 3, 8.5m, 8.2m, 25, 24);
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("INVALID_CREDITS");
    }

    [Fact]
    public void Create_WithBacklog_SetsHasBacklogAndRaisesEvent()
    {
        var r = Valid(earned: 20, attempted: 24);
        r.HasBacklog.Should().BeTrue();
        r.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentBacklogEvent>();
    }

    [Fact]
    public void Create_NoBacklog_DoesNotRaiseEvent()
    {
        var r = Valid();
        r.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Publish_SetsPublishedAtAndRaisesEvent()
    {
        var r = Valid();
        r.Publish();
        r.PublishedAt.Should().NotBeNull();
        r.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ResultDeclaredEvent>();
    }

    [Fact]
    public void Publish_AlreadyPublished_Throws()
    {
        var r = Valid();
        r.Publish();
        var act = () => r.Publish();
        act.Should().Throw<ExaminationDomainException>().Which.Code.Should().Be("ALREADY_PUBLISHED");
    }
}
