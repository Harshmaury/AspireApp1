using Examination.Application.ExamSchedule.Commands;
using Examination.Application.MarksEntry.Commands;
using Examination.Application.Interfaces;
using Examination.Domain.Entities;
using Examination.Domain.Enums;
using Examination.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Examination.Tests.Application;

// ─────────────────────────────────────────────────────────────
// CreateExamScheduleCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class CreateExamScheduleCommandHandlerTests
{
    static readonly Guid _tenant = Guid.NewGuid();
    static readonly Guid _course = Guid.NewGuid();

    static CreateExamScheduleCommand ValidCmd() => new(
        _tenant, _course, "2025-26", 3, "EndSem",
        DateTime.UtcNow.AddDays(10), 180, "Hall A", 100, 40);

    [Fact]
    public async Task Handle_ValidCommand_AddsScheduleAndReturnsId()
    {
        var repo = new Mock<IExamScheduleRepository>();
        var handler = new CreateExamScheduleCommandHandler(repo.Object);

        var id = await handler.Handle(ValidCmd(), CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<ExamSchedule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidExamType_Throws()
    {
        var repo = new Mock<IExamScheduleRepository>();
        var cmd = new CreateExamScheduleCommand(
            _tenant, _course, "2025-26", 3, "Written",
            DateTime.UtcNow.AddDays(10), 180, "Hall A", 100, 40);

        var handler = new CreateExamScheduleCommandHandler(repo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

// ─────────────────────────────────────────────────────────────
// EnterMarksCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class EnterMarksCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _exam    = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();
    static readonly Guid _by      = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_AddsEntryAndReturnsId()
    {
        var repo = new Mock<IMarksEntryRepository>();
        var cmd  = new EnterMarksCommand(_tenant, _student, _exam, _course, 75, 100, false, _by);

        var handler = new EnterMarksCommandHandler(repo.Object);
        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<MarksEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidMarks_ThrowsDomainException()
    {
        var repo = new Mock<IMarksEntryRepository>();
        var cmd  = new EnterMarksCommand(_tenant, _student, _exam, _course, 150, 100, false, _by);

        var handler = new EnterMarksCommandHandler(repo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ExaminationDomainException>()
            .Where(e => e.Code == "INVALID_MARKS");
    }
}

// ─────────────────────────────────────────────────────────────
// SubmitMarksCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class SubmitMarksCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _entryId = Guid.NewGuid();

    static MarksEntry DraftEntry() =>
        MarksEntry.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 75, 100, false, Guid.NewGuid());

    [Fact]
    public async Task Handle_ValidEntry_SubmitsAndUpdates()
    {
        var repo  = new Mock<IMarksEntryRepository>();
        var entry = DraftEntry();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var handler = new SubmitMarksCommandHandler(repo.Object);
        await handler.Handle(new SubmitMarksCommand(_tenant, _entryId), CancellationToken.None);

        entry.Status.Should().Be(MarksStatus.Submitted);
        repo.Verify(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IMarksEntryRepository>();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarksEntry?)null);

        var handler = new SubmitMarksCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new SubmitMarksCommand(_tenant, _entryId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────
// ApproveMarksCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class ApproveMarksCommandHandlerTests
{
    static readonly Guid _tenant     = Guid.NewGuid();
    static readonly Guid _entryId    = Guid.NewGuid();
    static readonly Guid _approvedBy = Guid.NewGuid();

    static MarksEntry SubmittedEntry()
    {
        var e = MarksEntry.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 75, 100, false, Guid.NewGuid());
        e.Submit();
        return e;
    }

    [Fact]
    public async Task Handle_SubmittedEntry_ApprovesAndUpdates()
    {
        var repo  = new Mock<IMarksEntryRepository>();
        var entry = SubmittedEntry();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var handler = new ApproveMarksCommandHandler(repo.Object);
        await handler.Handle(new ApproveMarksCommand(_tenant, _entryId, _approvedBy), CancellationToken.None);

        entry.Status.Should().Be(MarksStatus.Approved);
        repo.Verify(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IMarksEntryRepository>();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarksEntry?)null);

        var handler = new ApproveMarksCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new ApproveMarksCommand(_tenant, _entryId, _approvedBy), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────
// PublishMarksCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class PublishMarksCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _entryId = Guid.NewGuid();

    static MarksEntry ApprovedEntry()
    {
        var e = MarksEntry.Create(_tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 75, 100, false, Guid.NewGuid());
        e.Submit();
        e.Approve(Guid.NewGuid());
        return e;
    }

    [Fact]
    public async Task Handle_ApprovedEntry_PublishesAndUpdates()
    {
        var repo  = new Mock<IMarksEntryRepository>();
        var entry = ApprovedEntry();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var handler = new PublishMarksCommandHandler(repo.Object);
        await handler.Handle(new PublishMarksCommand(_tenant, _entryId), CancellationToken.None);

        entry.Status.Should().Be(MarksStatus.Published);
        repo.Verify(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = new Mock<IMarksEntryRepository>();
        repo.Setup(r => r.GetByIdAsync(_entryId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MarksEntry?)null);

        var handler = new PublishMarksCommandHandler(repo.Object);
        var act = async () => await handler.Handle(new PublishMarksCommand(_tenant, _entryId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
