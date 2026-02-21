using Student.Domain.Entities;
using Student.Domain.Enums;
using FluentAssertions;

namespace Student.Tests.Domain;

public sealed class StudentAggregateTests
{
    private static StudentAggregate ValidStudent() => StudentAggregate.Create(
        Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "john@uni.edu");

    [Fact]
    public void Create_ValidInput_ReturnsApplicant()
    {
        var s = ValidStudent();
        s.Should().NotBeNull();
        s.Status.Should().Be(StudentStatus.Applicant);
        s.StudentNumber.Should().StartWith("STU-");
    }

    [Fact]
    public void Create_EmailNormalisedToLower()
    {
        var s = StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "JOHN@UNI.EDU");
        s.Email.Should().Be("john@uni.edu");
    }

    [Fact]
    public void Create_EmptyFirstName_Throws()
    {
        var act = () => StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Doe", "j@uni.edu");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyLastName_Throws()
    {
        var act = () => StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "", "j@uni.edu");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        var act = () => StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RaisesStudentCreatedEvent()
    {
        var s = ValidStudent();
        s.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "StudentCreatedEvent");
    }

    [Fact]
    public void Admit_FromApplicant_SetsAdmittedStatus()
    {
        var s = ValidStudent();
        s.Admit();
        s.Status.Should().Be(StudentStatus.Admitted);
        s.AdmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void Admit_NotApplicant_Throws()
    {
        var s = ValidStudent();
        s.Admit();
        var act = () => s.Admit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Enroll_FromAdmitted_SetsEnrolledStatus()
    {
        var s = ValidStudent();
        s.Admit();
        s.Enroll();
        s.Status.Should().Be(StudentStatus.Enrolled);
        s.EnrolledAt.Should().NotBeNull();
    }

    [Fact]
    public void Enroll_NotAdmitted_Throws()
    {
        var s = ValidStudent();
        var act = () => s.Enroll();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Suspend_FromEnrolled_SetsSuspendedStatus()
    {
        var s = ValidStudent();
        s.Admit(); s.Enroll();
        s.Suspend("Academic misconduct");
        s.Status.Should().Be(StudentStatus.Suspended);
        s.SuspensionReason.Should().Be("Academic misconduct");
    }

    [Fact]
    public void Suspend_NotEnrolled_Throws()
    {
        var s = ValidStudent();
        s.Admit();
        var act = () => s.Suspend("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reinstate_FromSuspended_SetsEnrolled()
    {
        var s = ValidStudent();
        s.Admit(); s.Enroll(); s.Suspend("reason");
        s.Reinstate();
        s.Status.Should().Be(StudentStatus.Enrolled);
        s.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void Graduate_FromEnrolled_SetsAlumni()
    {
        var s = ValidStudent();
        s.Admit(); s.Enroll();
        s.Graduate();
        s.Status.Should().Be(StudentStatus.Alumni);
        s.GraduatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Archive_AlreadyArchived_Throws()
    {
        var s = ValidStudent();
        s.Archive();
        var act = () => s.Archive();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StateTransitions_RaiseStatusChangedEvents()
    {
        var s = ValidStudent();
        s.Admit();
        s.DomainEvents.Should().Contain(e => e.GetType().Name == "StudentStatusChangedEvent");
    }
}
