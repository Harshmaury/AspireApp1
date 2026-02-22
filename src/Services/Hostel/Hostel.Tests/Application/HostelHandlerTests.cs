using FluentAssertions;
using Moq;
using Hostel.Application.Allotments.Commands;
using Hostel.Application.Complaints.Commands;
using Hostel.Application.Hostels.Commands;
using Hostel.Application.Interfaces;
using Hostel.Application.Rooms.Commands;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Hostel.Domain.Exceptions;
using Xunit;
using HostelEntity = Hostel.Domain.Entities.Hostel;

namespace Hostel.Tests.Application;

public abstract class HandlerTestBase
{
    protected readonly Mock<IHostelUnitOfWork>    Uow           = new();
    protected readonly Mock<IHostelRepository>    HostelRepo    = new();
    protected readonly Mock<IRoomRepository>      RoomRepo      = new();
    protected readonly Mock<IAllotmentRepository> AllotmentRepo = new();
    protected readonly Mock<IComplaintRepository> ComplaintRepo = new();
    protected HandlerTestBase()
    {
        Uow.Setup(u => u.Hostels).Returns(HostelRepo.Object);
        Uow.Setup(u => u.Rooms).Returns(RoomRepo.Object);
        Uow.Setup(u => u.Allotments).Returns(AllotmentRepo.Object);
        Uow.Setup(u => u.Complaints).Returns(ComplaintRepo.Object);
        Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }
    protected static HostelEntity MakeHostelEntity() =>
        HostelEntity.Create(Guid.NewGuid(), "Block A", HostelType.Boys, 30, "Warden", "9876543210");
    protected static Room MakeRoom(int capacity = 2) =>
        Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, capacity);
    protected static RoomAllotment MakeAllotment(Guid? roomId = null) =>
        RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), roomId ?? Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
    protected static HostelComplaint MakeComplaint() =>
        HostelComplaint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Electrical, "Light not working.");
}

public sealed class CreateHostelHandlerTests : HandlerTestBase
{
    private readonly CreateHostelCommandHandler _h;
    public CreateHostelHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_ValidCommand_ReturnsHostelDto()
    {
        HostelRepo.Setup(r => r.AddAsync(It.IsAny<HostelEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _h.Handle(new CreateHostelCommand(Guid.NewGuid(), "Block B", HostelType.Girls, 40, "Ms. Patel", "9000000001"), default);
        result.Name.Should().Be("Block B");
        result.Type.Should().Be(HostelType.Girls);
        result.TotalRooms.Should().Be(40);
        result.IsActive.Should().BeTrue();
        result.Id.Should().NotBe(Guid.Empty);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Handle_SaveCalled_Once()
    {
        HostelRepo.Setup(r => r.AddAsync(It.IsAny<HostelEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _h.Handle(new CreateHostelCommand(Guid.NewGuid(), "H", HostelType.CoEd, 10, "W", "123"), default);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class UpdateWardenHandlerTests : HandlerTestBase
{
    private readonly UpdateWardenCommandHandler _h;
    public UpdateWardenHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_HostelFound_UpdatesAndSaves()
    {
        var hostel = MakeHostelEntity();
        HostelRepo.Setup(r => r.GetByIdAsync(hostel.Id, hostel.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(hostel);
        await _h.Handle(new UpdateWardenCommand(hostel.Id, hostel.TenantId, "New Warden", "9111111111"), default);
        hostel.WardenName.Should().Be("New Warden");
        hostel.WardenContact.Should().Be("9111111111");
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Handle_HostelNotFound_Throws()
    {
        HostelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((HostelEntity?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new UpdateWardenCommand(Guid.NewGuid(), Guid.NewGuid(), "W", "123"), default));
        ex.Code.Should().Be("HOSTEL_NOT_FOUND");
    }
}

public sealed class CreateRoomHandlerTests : HandlerTestBase
{
    private readonly CreateRoomCommandHandler _h;
    public CreateRoomHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_ValidRoom_ReturnsRoomDto()
    {
        var hostel = MakeHostelEntity();
        HostelRepo.Setup(r => r.GetByIdAsync(hostel.Id, hostel.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(hostel);
        RoomRepo.Setup(r => r.RoomNumberExistsAsync(hostel.Id, "101", hostel.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        RoomRepo.Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _h.Handle(new CreateRoomCommand(hostel.TenantId, hostel.Id, "101", 1, RoomType.Double, 2), default);
        result.RoomNumber.Should().Be("101");
        result.Capacity.Should().Be(2);
        result.Status.Should().Be(RoomStatus.Available);
    }
    [Fact]
    public async Task Handle_HostelNotFound_Throws()
    {
        HostelRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((HostelEntity?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new CreateRoomCommand(Guid.NewGuid(), Guid.NewGuid(), "101", 0, RoomType.Single, 1), default));
        ex.Code.Should().Be("HOSTEL_NOT_FOUND");
    }
    [Fact]
    public async Task Handle_DuplicateRoomNumber_Throws()
    {
        var hostel = MakeHostelEntity();
        HostelRepo.Setup(r => r.GetByIdAsync(hostel.Id, hostel.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(hostel);
        RoomRepo.Setup(r => r.RoomNumberExistsAsync(hostel.Id, "101", hostel.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new CreateRoomCommand(hostel.TenantId, hostel.Id, "101", 0, RoomType.Single, 1), default));
        ex.Code.Should().Be("ROOM_EXISTS");
    }
}

public sealed class SetMaintenanceHandlerTests : HandlerTestBase
{
    private readonly SetMaintenanceCommandHandler _h;
    public SetMaintenanceHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_SetTrue_RoomUnderMaintenance()
    {
        var room = MakeRoom();
        RoomRepo.Setup(r => r.GetByIdAsync(room.Id, room.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        await _h.Handle(new SetMaintenanceCommand(room.Id, room.TenantId, true), default);
        room.Status.Should().Be(RoomStatus.UnderMaintenance);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Handle_SetFalse_ClearsMaintenance()
    {
        var room = MakeRoom();
        room.SetMaintenance();
        RoomRepo.Setup(r => r.GetByIdAsync(room.Id, room.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        await _h.Handle(new SetMaintenanceCommand(room.Id, room.TenantId, false), default);
        room.Status.Should().Be(RoomStatus.Available);
    }
    [Fact]
    public async Task Handle_RoomNotFound_Throws()
    {
        RoomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Room?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new SetMaintenanceCommand(Guid.NewGuid(), Guid.NewGuid(), true), default));
        ex.Code.Should().Be("ROOM_NOT_FOUND");
    }
}

public sealed class AllocateRoomHandlerTests : HandlerTestBase
{
    private readonly AllocateRoomCommandHandler _h;
    public AllocateRoomHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_ValidAllocation_ReturnsAllotmentDto()
    {
        var tid  = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var room = MakeRoom(capacity: 2);
        AllotmentRepo.Setup(r => r.GetActiveByStudentAsync(sid, "2024-25", tid, It.IsAny<CancellationToken>())).ReturnsAsync((RoomAllotment?)null);
        RoomRepo.Setup(r => r.GetByIdAsync(room.Id, tid, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        AllotmentRepo.Setup(r => r.AddAsync(It.IsAny<RoomAllotment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _h.Handle(new AllocateRoomCommand(tid, sid, room.Id, Guid.NewGuid(), "2024-25", 1), default);
        result.StudentId.Should().Be(sid);
        result.AcademicYear.Should().Be("2024-25");
        room.CurrentOccupancy.Should().Be(1);
    }
    [Fact]
    public async Task Handle_AlreadyAllotted_Throws()
    {
        var tid = Guid.NewGuid();
        var sid = Guid.NewGuid();
        AllotmentRepo.Setup(r => r.GetActiveByStudentAsync(sid, "2024-25", tid, It.IsAny<CancellationToken>())).ReturnsAsync(MakeAllotment());
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new AllocateRoomCommand(tid, sid, Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1), default));
        ex.Code.Should().Be("ALREADY_ALLOTTED");
    }
    [Fact]
    public async Task Handle_RoomNotFound_Throws()
    {
        var tid = Guid.NewGuid();
        AllotmentRepo.Setup(r => r.GetActiveByStudentAsync(It.IsAny<Guid>(), "2024-25", tid, It.IsAny<CancellationToken>())).ReturnsAsync((RoomAllotment?)null);
        RoomRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), tid, It.IsAny<CancellationToken>())).ReturnsAsync((Room?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new AllocateRoomCommand(tid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1), default));
        ex.Code.Should().Be("ROOM_NOT_FOUND");
    }
}

public sealed class VacateRoomHandlerTests : HandlerTestBase
{
    private readonly VacateRoomCommandHandler _h;
    public VacateRoomHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_ValidVacate_VacatesAllotmentAndDecrementsRoom()
    {
        var room      = MakeRoom(capacity: 2);
        room.IncrementOccupancy();
        var allotment = MakeAllotment(room.Id);
        var tid       = allotment.TenantId;
        AllotmentRepo.Setup(r => r.GetByIdAsync(allotment.Id, tid, It.IsAny<CancellationToken>())).ReturnsAsync(allotment);
        RoomRepo.Setup(r => r.GetByIdAsync(room.Id, tid, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        await _h.Handle(new VacateRoomCommand(allotment.Id, tid), default);
        allotment.Status.Should().Be(AllotmentStatus.Vacated);
        room.CurrentOccupancy.Should().Be(0);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Handle_AllotmentNotFound_Throws()
    {
        AllotmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoomAllotment?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new VacateRoomCommand(Guid.NewGuid(), Guid.NewGuid()), default));
        ex.Code.Should().Be("ALLOTMENT_NOT_FOUND");
    }
}

public sealed class RaiseComplaintHandlerTests : HandlerTestBase
{
    private readonly RaiseComplaintCommandHandler _h;
    public RaiseComplaintHandlerTests() => _h = new(Uow.Object);
    [Fact]
    public async Task Handle_ValidComplaint_ReturnsComplaintDto()
    {
        ComplaintRepo.Setup(r => r.AddAsync(It.IsAny<HostelComplaint>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _h.Handle(new RaiseComplaintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Security, "Gate not locked."), default);
        result.Category.Should().Be(ComplaintCategory.Security);
        result.Status.Should().Be(ComplaintStatus.Open);
        result.Id.Should().NotBe(Guid.Empty);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Handle_SaveCalled_Once()
    {
        ComplaintRepo.Setup(r => r.AddAsync(It.IsAny<HostelComplaint>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _h.Handle(new RaiseComplaintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Mess, "Bad food."), default);
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class UpdateComplaintStatusHandlerTests : HandlerTestBase
{
    private readonly UpdateComplaintStatusCommandHandler _h;
    public UpdateComplaintStatusHandlerTests() => _h = new(Uow.Object);
    private void Setup(HostelComplaint c) =>
        ComplaintRepo.Setup(r => r.GetByIdAsync(c.Id, c.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(c);
    [Fact]
    public async Task Handle_InProgress_SetsStatus()
    {
        var c = MakeComplaint(); Setup(c);
        await _h.Handle(new UpdateComplaintStatusCommand(c.Id, c.TenantId, "InProgress"), default);
        c.Status.Should().Be(ComplaintStatus.InProgress);
    }
    [Fact]
    public async Task Handle_Resolve_WithNote_SetsResolved()
    {
        var c = MakeComplaint(); Setup(c);
        await _h.Handle(new UpdateComplaintStatusCommand(c.Id, c.TenantId, "Resolve", "Fixed."), default);
        c.Status.Should().Be(ComplaintStatus.Resolved);
        c.ResolutionNote.Should().Be("Fixed.");
    }
    [Fact]
    public async Task Handle_Resolve_WithoutNote_Throws()
    {
        var c = MakeComplaint(); Setup(c);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new UpdateComplaintStatusCommand(c.Id, c.TenantId, "Resolve", null), default));
        ex.Code.Should().Be("NOTE_REQUIRED");
    }
    [Fact]
    public async Task Handle_Close_AfterResolve_SetsClosed()
    {
        var c = MakeComplaint(); c.Resolve("Done."); Setup(c);
        await _h.Handle(new UpdateComplaintStatusCommand(c.Id, c.TenantId, "Close"), default);
        c.Status.Should().Be(ComplaintStatus.Closed);
    }
    [Fact]
    public async Task Handle_InvalidAction_Throws()
    {
        var c = MakeComplaint(); Setup(c);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new UpdateComplaintStatusCommand(c.Id, c.TenantId, "Explode"), default));
        ex.Code.Should().Be("INVALID_ACTION");
    }
    [Fact]
    public async Task Handle_ComplaintNotFound_Throws()
    {
        ComplaintRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((HostelComplaint?)null);
        var ex = await Assert.ThrowsAsync<HostelDomainException>(() =>
            _h.Handle(new UpdateComplaintStatusCommand(Guid.NewGuid(), Guid.NewGuid(), "InProgress"), default));
        ex.Code.Should().Be("COMPLAINT_NOT_FOUND");
    }
}
