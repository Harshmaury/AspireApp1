using Attendance.Application.AttendanceRecord.Commands;
using Attendance.Application.Condonation.Commands;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Domain.Enums;
using Attendance.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Attendance.Tests.Application;

// ─────────────────────────────────────────────────────────────
// MarkAttendanceCommandHandler — 3 tests
// ─────────────────────────────────────────────────────────────
public class MarkAttendanceCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();
    static readonly Guid _marker  = Guid.NewGuid();
    static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    static MarkAttendanceCommand ValidCmd() => new(
        _tenant, _student, _course, "2025-26", 3, _today, "Lecture", true, _marker);

    [Fact]
    public async Task Handle_ValidCommand_AddsRecordAndReturnId()
    {
        var recordRepo  = new Mock<IAttendanceRecordRepository>();
        var summaryRepo = new Mock<IAttendanceSummaryRepository>();

        recordRepo.Setup(r => r.GetCountsAsync(_student, _course, _tenant, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((5, 4));
        summaryRepo.Setup(r => r.GetByStudentCourseAsync(_student, _course, _tenant, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((AttendanceSummary?)null);

        var handler = new MarkAttendanceCommandHandler(recordRepo.Object, summaryRepo.Object);
        var id = await handler.Handle(ValidCmd(), CancellationToken.None);

        id.Should().NotBeEmpty();
        recordRepo.Verify(r => r.AddAsync(It.IsAny<AttendanceRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        summaryRepo.Verify(r => r.AddAsync(It.IsAny<AttendanceSummary>(), It.IsAny<CancellationToken>()), Times.Once);
        summaryRepo.Verify(r => r.UpdateAsync(It.IsAny<AttendanceSummary>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingSummary_UpdatesWithoutAdding()
    {
        var recordRepo  = new Mock<IAttendanceRecordRepository>();
        var summaryRepo = new Mock<IAttendanceSummaryRepository>();
        var existingSummary = AttendanceSummary.Create(_tenant, _student, _course, "2025-26", 3);

        recordRepo.Setup(r => r.GetCountsAsync(_student, _course, _tenant, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((10, 9));
        summaryRepo.Setup(r => r.GetByStudentCourseAsync(_student, _course, _tenant, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(existingSummary);

        var handler = new MarkAttendanceCommandHandler(recordRepo.Object, summaryRepo.Object);
        await handler.Handle(ValidCmd(), CancellationToken.None);

        summaryRepo.Verify(r => r.AddAsync(It.IsAny<AttendanceSummary>(), It.IsAny<CancellationToken>()), Times.Never);
        summaryRepo.Verify(r => r.UpdateAsync(existingSummary, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidClassType_Throws()
    {
        var recordRepo  = new Mock<IAttendanceRecordRepository>();
        var summaryRepo = new Mock<IAttendanceSummaryRepository>();
        var cmd = new MarkAttendanceCommand(_tenant, _student, _course, "2025-26", 3, _today, "Seminar", true, _marker);

        var handler = new MarkAttendanceCommandHandler(recordRepo.Object, summaryRepo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

// ─────────────────────────────────────────────────────────────
// CreateCondonationRequestCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class CreateCondonationRequestCommandHandlerTests
{
    static readonly Guid _tenant  = Guid.NewGuid();
    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _course  = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_AddsRequestAndReturnsId()
    {
        var repo = new Mock<ICondonationRequestRepository>();
        var cmd  = new CreateCondonationRequestCommand(_tenant, _student, _course, "Medical leave");

        var handler = new CreateCondonationRequestCommandHandler(repo.Object);
        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<CondonationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyReason_ThrowsDomainException()
    {
        var repo = new Mock<ICondonationRequestRepository>();
        var cmd  = new CreateCondonationRequestCommand(_tenant, _student, _course, "  ");

        var handler = new CreateCondonationRequestCommandHandler(repo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AttendanceDomainException>()
            .Where(e => e.Code == "INVALID_REASON");
    }
}

// ─────────────────────────────────────────────────────────────
// ApproveCondonationCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class ApproveCondonationCommandHandlerTests
{
    static readonly Guid _tenant     = Guid.NewGuid();
    static readonly Guid _student    = Guid.NewGuid();
    static readonly Guid _course     = Guid.NewGuid();
    static readonly Guid _reviewedBy = Guid.NewGuid();
    static readonly Guid _requestId  = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidRequest_ApprovesAndUpdates()
    {
        var repo    = new Mock<ICondonationRequestRepository>();
        var request = CondonationRequest.Create(_tenant, _student, _course, "Medical leave");
        var cmd     = new ApproveCondonationCommand(_tenant, _requestId, _reviewedBy, "Approved with docs");

        repo.Setup(r => r.GetByIdAsync(_requestId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var handler = new ApproveCondonationCommandHandler(repo.Object);
        await handler.Handle(cmd, CancellationToken.None);

        request.Status.Should().Be(CondonationStatus.Approved);
        repo.Verify(r => r.UpdateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsDomainException()
    {
        var repo = new Mock<ICondonationRequestRepository>();
        var cmd  = new ApproveCondonationCommand(_tenant, _requestId, _reviewedBy);

        repo.Setup(r => r.GetByIdAsync(_requestId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CondonationRequest?)null);

        var handler = new ApproveCondonationCommandHandler(repo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AttendanceDomainException>()
            .Where(e => e.Code == "NOT_FOUND");
    }
}

// ─────────────────────────────────────────────────────────────
// RejectCondonationCommandHandler — 2 tests
// ─────────────────────────────────────────────────────────────
public class RejectCondonationCommandHandlerTests
{
    static readonly Guid _tenant     = Guid.NewGuid();
    static readonly Guid _student    = Guid.NewGuid();
    static readonly Guid _course     = Guid.NewGuid();
    static readonly Guid _reviewedBy = Guid.NewGuid();
    static readonly Guid _requestId  = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidRequest_RejectsAndUpdates()
    {
        var repo    = new Mock<ICondonationRequestRepository>();
        var request = CondonationRequest.Create(_tenant, _student, _course, "Medical leave");
        var cmd     = new RejectCondonationCommand(_tenant, _requestId, _reviewedBy, "Insufficient proof");

        repo.Setup(r => r.GetByIdAsync(_requestId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var handler = new RejectCondonationCommandHandler(repo.Object);
        await handler.Handle(cmd, CancellationToken.None);

        request.Status.Should().Be(CondonationStatus.Rejected);
        repo.Verify(r => r.UpdateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsDomainException()
    {
        var repo = new Mock<ICondonationRequestRepository>();
        var cmd  = new RejectCondonationCommand(_tenant, _requestId, _reviewedBy, "No docs");

        repo.Setup(r => r.GetByIdAsync(_requestId, _tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CondonationRequest?)null);

        var handler = new RejectCondonationCommandHandler(repo.Object);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AttendanceDomainException>()
            .Where(e => e.Code == "NOT_FOUND");
    }
}
