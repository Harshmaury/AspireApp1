using FluentAssertions;
using Hostel.Domain.Entities;
using Hostel.Domain.Enums;
using Hostel.Infrastructure.Persistence;
using Hostel.Infrastructure.Persistence.Repositories;
using TenantIsolation.Tests.Helpers;
using Xunit;
using HostelEntity = Hostel.Domain.Entities.Hostel;

namespace TenantIsolation.Tests.Hostel;

public sealed class HostelIsolationTests
{
    private static HostelDbContext MakeDb(string name) =>
        DbFactory.Create<HostelDbContext>(o => new HostelDbContext(o), name);

    private static HostelEntity   MakeHostel(Guid tid)     => HostelEntity.Create(tid, "Block A", HostelType.Boys, 20, "Warden", "9999999999");
    private static Room           MakeRoom(Guid tid)        => Room.Create(tid, Guid.NewGuid(), "101", 1, RoomType.Double, 2);
    private static RoomAllotment  MakeAllotment(Guid tid)   => RoomAllotment.Create(tid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
    private static HostelComplaint MakeComplaint(Guid tid)  => HostelComplaint.Create(tid, Guid.NewGuid(), Guid.NewGuid(), ComplaintCategory.Maintenance, "Issue.");

    // ── Hostel ────────────────────────────────────────────────

    [Fact]
    public async Task Hostel_GetById_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var h = MakeHostel(tid);
        using (var ctx = MakeDb(db)) { ctx.Hostels.Add(h); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new HostelRepository(q).GetByIdAsync(h.Id, tid)).Should().NotBeNull();
    }

    [Fact]
    public async Task Hostel_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var hA = MakeHostel(Guid.NewGuid()); var hB = MakeHostel(Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.Hostels.AddRange(hA, hB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new HostelRepository(q).GetByIdAsync(hA.Id, hB.TenantId)).Should().BeNull();
    }

    // ── Room ──────────────────────────────────────────────────

    [Fact]
    public async Task Room_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var rA = MakeRoom(Guid.NewGuid()); var rB = MakeRoom(Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.Rooms.AddRange(rA, rB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new RoomRepository(q).GetByIdAsync(rA.Id, rB.TenantId)).Should().BeNull();
    }

    [Fact]
    public async Task Room_GetById_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var r = MakeRoom(tid);
        using (var ctx = MakeDb(db)) { ctx.Rooms.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new RoomRepository(q).GetByIdAsync(r.Id, tid)).Should().NotBeNull();
    }

    // ── Allotment ─────────────────────────────────────────────

    [Fact]
    public async Task Allotment_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var aA = MakeAllotment(Guid.NewGuid()); var aB = MakeAllotment(Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.RoomAllotments.AddRange(aA, aB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AllotmentRepository(q).GetByIdAsync(aA.Id, aB.TenantId)).Should().BeNull();
    }

    [Fact]
    public async Task Allotment_GetActiveByStudent_CrossTenant_ReturnsNull()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var a    = RoomAllotment.Create(tidA, sid, Guid.NewGuid(), Guid.NewGuid(), "2024-25", 1);
        using (var ctx = MakeDb(db)) { ctx.RoomAllotments.Add(a); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AllotmentRepository(q).GetActiveByStudentAsync(sid, "2024-25", tidB)).Should().BeNull();
    }

    // ── Complaint ─────────────────────────────────────────────

    [Fact]
    public async Task Complaint_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var cA = MakeComplaint(Guid.NewGuid()); var cB = MakeComplaint(Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.HostelComplaints.AddRange(cA, cB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ComplaintRepository(q).GetByIdAsync(cA.Id, cB.TenantId)).Should().BeNull();
    }

    [Fact]
    public async Task Complaint_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var c    = HostelComplaint.Create(tidA, sid, Guid.NewGuid(), ComplaintCategory.Security, "Gate open.");
        using (var ctx = MakeDb(db)) { ctx.HostelComplaints.Add(c); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ComplaintRepository(q).GetByStudentAsync(sid, tidB)).Should().BeEmpty();
    }
}
