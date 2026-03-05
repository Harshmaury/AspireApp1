using FluentAssertions;
using Moq;
using Student.Application.Features.Students.Commands;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using Student.Domain.Enums;

namespace Student.Tests.Application;

public sealed class StudentLifecycleCommandHandlerTests
{
    private readonly Mock<IStudentRepository> _repo = new();

    private static StudentAggregate Applicant() =>
        StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "j@uni.edu");

    private static StudentAggregate Admitted()
    { var s = Applicant(); s.Admit(); return s; }

    private static StudentAggregate Enrolled()
    { var s = Admitted(); s.Enroll(); return s; }

    private static StudentAggregate Suspended()
    { var s = Enrolled(); s.Suspend("Misconduct"); return s; }

    // ── Enroll ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Enroll_AdmittedStudent_SetsEnrolled()
    {
        var student = Admitted();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new EnrollStudentCommandHandler(_repo.Object)
            .Handle(new EnrollStudentCommand(student.Id, student.TenantId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.Status.Should().Be(StudentStatus.Enrolled);
    }

    [Fact]
    public async Task Enroll_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new EnrollStudentCommandHandler(_repo.Object)
            .Handle(new EnrollStudentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Suspend ───────────────────────────────────────────────────────────
    [Fact]
    public async Task Suspend_EnrolledStudent_SetsSuspended()
    {
        var student = Enrolled();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new SuspendStudentCommandHandler(_repo.Object)
            .Handle(new SuspendStudentCommand(student.Id, student.TenantId, "Academic misconduct"), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.Status.Should().Be(StudentStatus.Suspended);
        student.SuspensionReason.Should().Be("Academic misconduct");
    }

    [Fact]
    public async Task Suspend_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new SuspendStudentCommandHandler(_repo.Object)
            .Handle(new SuspendStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "reason"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Reinstate ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Reinstate_SuspendedStudent_SetsEnrolled()
    {
        var student = Suspended();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new ReinstateStudentCommandHandler(_repo.Object)
            .Handle(new ReinstateStudentCommand(student.Id, student.TenantId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.Status.Should().Be(StudentStatus.Enrolled);
        student.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public async Task Reinstate_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new ReinstateStudentCommandHandler(_repo.Object)
            .Handle(new ReinstateStudentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Graduate ──────────────────────────────────────────────────────────
    [Fact]
    public async Task Graduate_EnrolledStudent_SetsAlumni()
    {
        var student = Enrolled();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new GraduateStudentCommandHandler(_repo.Object)
            .Handle(new GraduateStudentCommand(student.Id, student.TenantId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.Status.Should().Be(StudentStatus.Alumni);
        student.GraduatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Graduate_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new GraduateStudentCommandHandler(_repo.Object)
            .Handle(new GraduateStudentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── Archive ───────────────────────────────────────────────────────────
    [Fact]
    public async Task Archive_AnyStudent_SetsArchived()
    {
        var student = Enrolled();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new ArchiveStudentCommandHandler(_repo.Object)
            .Handle(new ArchiveStudentCommand(student.Id, student.TenantId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.Status.Should().Be(StudentStatus.Archived);
    }

    [Fact]
    public async Task Archive_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new ArchiveStudentCommandHandler(_repo.Object)
            .Handle(new ArchiveStudentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    // ── UpdateDetails ─────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateDetails_ValidData_UpdatesAndRaisesEvent()
    {
        var student = Enrolled();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var act = async () => await new UpdateStudentCommandHandler(_repo.Object)
            .Handle(new UpdateStudentCommand(student.Id, student.TenantId, "Jane", "Smith", "jane@uni.edu"), CancellationToken.None);

        await act.Should().NotThrowAsync();
        student.FirstName.Should().Be("Jane");
        student.LastName.Should().Be("Smith");
        student.Email.Should().Be("jane@uni.edu");
        student.DomainEvents.Should().Contain(e => e.GetType().Name == "StudentDetailsUpdatedEvent");
    }

    [Fact]
    public async Task UpdateDetails_StudentNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await new UpdateStudentCommandHandler(_repo.Object)
            .Handle(new UpdateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "Jane", "Smith", "jane@uni.edu"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }
}
