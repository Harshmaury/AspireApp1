using FluentAssertions;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Hostel.Domain.Events;
using Hostel.Domain.Exceptions;
using Xunit;
using HostelEntity = Hostel.Domain.Entities.Hostel;

namespace Hostel.Tests.Domain;

public sealed class HostelEntityTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsActiveHostel()
    {
        var tid = Guid.NewGuid();
        var h = HostelEntity.Create(tid, "Boys Block A", HostelType.Boys, 50, "Mr. Singh", "9876543210");
        h.TenantId.Should().Be(tid);
        h.Name.Should().Be("Boys Block A");
        h.Type.Should().Be(HostelType.Boys);
        h.TotalRooms.Should().Be(50);
        h.IsActive.Should().BeTrue();
        h.Id.Should().NotBe(Guid.Empty);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelEntity.Create(Guid.NewGuid(), name, HostelType.Boys, 10, "Warden", "1234567890"));
        ex.Code.Should().Be("INVALID_NAME");
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_InvalidTotalRooms_Throws(int rooms)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelEntity.Create(Guid.NewGuid(), "Hostel", HostelType.Boys, rooms, "Warden", "123"));
        ex.Code.Should().Be("INVALID_ROOMS");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyWardenName_Throws(string name)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelEntity.Create(Guid.NewGuid(), "Hostel", HostelType.Boys, 10, name, "123"));
        ex.Code.Should().Be("INVALID_WARDEN");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyWardenContact_Throws(string contact)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelEntity.Create(Guid.NewGuid(), "Hostel", HostelType.Boys, 10, "Warden", contact));
        ex.Code.Should().Be("INVALID_CONTACT");
    }
    [Fact]
    public void UpdateWarden_ValidArgs_UpdatesBoth()
    {
        var h = HostelEntity.Create(Guid.NewGuid(), "H", HostelType.Boys, 10, "Old", "000");
        h.UpdateWarden("Mrs. Sharma", "9999999999");
        h.WardenName.Should().Be("Mrs. Sharma");
        h.WardenContact.Should().Be("9999999999");
    }
    [Fact]
    public void Deactivate_ActiveHostel_SetsInactive()
    {
        var h = HostelEntity.Create(Guid.NewGuid(), "H", HostelType.Boys, 10, "W", "123");
        h.Deactivate();
        h.IsActive.Should().BeFalse();
    }
    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var h = HostelEntity.Create(Guid.NewGuid(), "H", HostelType.Boys, 10, "W", "123");
        h.Deactivate();
        var ex = Assert.Throws<HostelDomainException>(() => h.Deactivate());
        ex.Code.Should().Be("ALREADY_INACTIVE");
    }
}

public sealed class RoomTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsAvailableRoom()
    {
        var tid = Guid.NewGuid();
        var hid = Guid.NewGuid();
        var r = Room.Create(tid, hid, "101", 1, RoomType.Double, 2);
        r.TenantId.Should().Be(tid);
        r.HostelId.Should().Be(hid);
        r.RoomNumber.Should().Be("101");
        r.Capacity.Should().Be(2);
        r.CurrentOccupancy.Should().Be(0);
        r.Status.Should().Be(RoomStatus.Available);
        r.Id.Should().NotBe(Guid.Empty);
    }
    [Fact]
    public void Create_RoomNumberStoredUpperCase()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "a1b", 0, RoomType.Single, 1);
        r.RoomNumber.Should().Be("A1B");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyRoomNumber_Throws(string num)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            Room.Create(Guid.NewGuid(), Guid.NewGuid(), num, 0, RoomType.Single, 1));
        ex.Code.Should().Be("INVALID_ROOM_NUMBER");
    }
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Create_InvalidCapacity_Throws(int cap)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 0, RoomType.Single, cap));
        ex.Code.Should().Be("INVALID_CAPACITY");
    }
    [Fact]
    public void Create_NegativeFloor_Throws()
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", -1, RoomType.Single, 1));
        ex.Code.Should().Be("INVALID_FLOOR");
    }
    [Fact]
    public void IncrementOccupancy_BelowCapacity_Increments()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, 2);
        r.IncrementOccupancy();
        r.CurrentOccupancy.Should().Be(1);
        r.Status.Should().Be(RoomStatus.Available);
    }
    [Fact]
    public void IncrementOccupancy_ReachesCapacity_SetsFullyOccupied()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Single, 1);
        r.IncrementOccupancy();
        r.Status.Should().Be(RoomStatus.FullyOccupied);
    }
    [Fact]
    public void IncrementOccupancy_AtCapacity_Throws()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Single, 1);
        r.IncrementOccupancy();
        var ex = Assert.Throws<HostelDomainException>(() => r.IncrementOccupancy());
        ex.Code.Should().Be("ROOM_FULL");
    }
    [Fact]
    public void DecrementOccupancy_FromFullyOccupied_SetsAvailable()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Single, 1);
        r.IncrementOccupancy();
        r.DecrementOccupancy();
        r.CurrentOccupancy.Should().Be(0);
        r.Status.Should().Be(RoomStatus.Available);
    }
    [Fact]
    public void DecrementOccupancy_AtZero_Throws()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, 2);
        var ex = Assert.Throws<HostelDomainException>(() => r.DecrementOccupancy());
        ex.Code.Should().Be("INVALID_OCCUPANCY");
    }
    [Fact]
    public void SetMaintenance_EmptyRoom_SetsUnderMaintenance()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, 2);
        r.SetMaintenance();
        r.Status.Should().Be(RoomStatus.UnderMaintenance);
    }
    [Fact]
    public void SetMaintenance_OccupiedRoom_Throws()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, 2);
        r.IncrementOccupancy();
        var ex = Assert.Throws<HostelDomainException>(() => r.SetMaintenance());
        ex.Code.Should().Be("ROOM_OCCUPIED");
    }
    [Fact]
    public void ClearMaintenance_UnderMaintenance_SetsAvailable()
    {
        var r = Room.Create(Guid.NewGuid(), Guid.NewGuid(), "101", 1, RoomType.Double, 2);
        r.SetMaintenance();
        r.ClearMaintenance();
        r.Status.Should().Be(RoomStatus.Available);
    }
}

public sealed class RoomAllotmentTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsActiveAllotment()
    {
        var tid = Guid.NewGuid();
        var a = RoomAllotment.Create(tid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.TenantId.Should().Be(tid);
        a.AcademicYear.Should().Be("2024-25");
        a.BedNumber.Should().Be(1);
        a.Status.Should().Be(AllotmentStatus.Active);
        a.VacatedAt.Should().BeNull();
        a.Id.Should().NotBe(Guid.Empty);
    }
    [Fact]
    public void Create_RaisesRoomAllottedEvent()
    {
        var a = RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoomAllottedEvent>();
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAcademicYear_Throws(string year)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), year, 1));
        ex.Code.Should().Be("INVALID_YEAR");
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidBedNumber_Throws(int bed)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", bed));
        ex.Code.Should().Be("INVALID_BED");
    }
    [Fact]
    public void Vacate_ActiveAllotment_SetsVacatedAndRaisesEvent()
    {
        var a = RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.ClearDomainEvents();
        a.Vacate();
        a.Status.Should().Be(AllotmentStatus.Vacated);
        a.VacatedAt.Should().NotBeNull();
        a.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoomVacatedEvent>();
    }
    [Fact]
    public void Vacate_AlreadyVacated_Throws()
    {
        var a = RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.Vacate();
        var ex = Assert.Throws<HostelDomainException>(() => a.Vacate());
        ex.Code.Should().Be("NOT_ACTIVE");
    }
    [Fact]
    public void Cancel_ActiveAllotment_SetsCancelled()
    {
        var a = RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.Cancel();
        a.Status.Should().Be(AllotmentStatus.Cancelled);
        a.VacatedAt.Should().NotBeNull();
    }
    [Fact]
    public void Cancel_AfterVacate_Throws()
    {
        var a = RoomAllotment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        a.Vacate();
        var ex = Assert.Throws<HostelDomainException>(() => a.Cancel());
        ex.Code.Should().Be("NOT_ACTIVE");
    }
}

public sealed class HostelComplaintTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsOpenComplaint()
    {
        var tid = Guid.NewGuid();
        var c = HostelComplaint.Create(tid, Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Plumbing, "Water leak.");
        c.TenantId.Should().Be(tid);
        c.Category.Should().Be(ComplaintCategory.Plumbing);
        c.Status.Should().Be(ComplaintStatus.Open);
        c.ResolutionNote.Should().BeNull();
        c.ResolvedAt.Should().BeNull();
        c.Id.Should().NotBe(Guid.Empty);
    }
    [Fact]
    public void Create_RaisesComplaintRaisedEvent()
    {
        var c = HostelComplaint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Other, "Issue.");
        c.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ComplaintRaisedEvent>();
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyDescription_Throws(string desc)
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelComplaint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Other, desc));
        ex.Code.Should().Be("INVALID_DESCRIPTION");
    }
    [Fact]
    public void Create_DescriptionOver1000Chars_Throws()
    {
        var ex = Assert.Throws<HostelDomainException>(() =>
            HostelComplaint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Other, new string('x', 1001)));
        ex.Code.Should().Be("DESCRIPTION_TOO_LONG");
    }
    [Fact]
    public void MarkInProgress_OpenComplaint_SetsInProgress()
    {
        var c = MakeComplaint();
        c.MarkInProgress();
        c.Status.Should().Be(ComplaintStatus.InProgress);
    }
    [Fact]
    public void MarkInProgress_NonOpenComplaint_Throws()
    {
        var c = MakeComplaint();
        c.MarkInProgress();
        var ex = Assert.Throws<HostelDomainException>(() => c.MarkInProgress());
        ex.Code.Should().Be("INVALID_STATUS");
    }
    [Fact]
    public void Resolve_OpenComplaint_SetsResolvedAndRaisesEvent()
    {
        var c = MakeComplaint();
        c.ClearDomainEvents();
        c.Resolve("Fixed the leak.");
        c.Status.Should().Be(ComplaintStatus.Resolved);
        c.ResolutionNote.Should().Be("Fixed the leak.");
        c.ResolvedAt.Should().NotBeNull();
        c.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ComplaintResolvedEvent>();
    }
    [Fact]
    public void Resolve_InProgressComplaint_SetsResolved()
    {
        var c = MakeComplaint();
        c.MarkInProgress();
        c.Resolve("Done.");
        c.Status.Should().Be(ComplaintStatus.Resolved);
    }
    [Fact]
    public void Resolve_AlreadyResolved_Throws()
    {
        var c = MakeComplaint();
        c.Resolve("Done.");
        var ex = Assert.Throws<HostelDomainException>(() => c.Resolve("Again."));
        ex.Code.Should().Be("ALREADY_RESOLVED");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_EmptyNote_Throws(string note)
    {
        var c = MakeComplaint();
        var ex = Assert.Throws<HostelDomainException>(() => c.Resolve(note));
        ex.Code.Should().Be("NOTE_REQUIRED");
    }
    [Fact]
    public void Close_ResolvedComplaint_SetsClosed()
    {
        var c = MakeComplaint();
        c.Resolve("Fixed.");
        c.Close();
        c.Status.Should().Be(ComplaintStatus.Closed);
    }
    [Fact]
    public void Close_NotResolved_Throws()
    {
        var c = MakeComplaint();
        var ex = Assert.Throws<HostelDomainException>(() => c.Close());
        ex.Code.Should().Be("NOT_RESOLVED");
    }
    private static HostelComplaint MakeComplaint() =>
        HostelComplaint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Maintenance, "Broken door.");
}
