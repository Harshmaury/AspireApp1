using Attendance.Domain.Entities;
using Attendance.Domain.Enums;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using TenantIsolation.Tests.Helpers;
using Xunit;

namespace TenantIsolation.Tests.Attendance;

public sealed class AttendanceIsolationTests
{
    private static AttendanceDbContext MakeDb(string name) =>
        DbFactory.Create<AttendanceDbContext>(o => new AttendanceDbContext(o), name);

    // Use today — passes both future and backdating guards
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static AttendanceRecord MakeRecord(Guid tid, Guid sid, Guid cid) =>
        AttendanceRecord.Create(tid, sid, cid,
            "2024-25", 1, Today,
            ClassType.Lecture, true, Guid.NewGuid());

    private static CondonationRequest MakeCondonation(Guid tid, Guid sid) =>
        CondonationRequest.Create(tid, sid, Guid.NewGuid(), "Medical emergency.");

    // ── AttendanceRecord ──────────────────────────────────────

    [Fact]
    public async Task AttendanceRecord_GetById_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var r = MakeRecord(tid, Guid.NewGuid(), Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.AttendanceRecords.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AttendanceRecordRepository(q).GetByIdAsync(r.Id, tid)).Should().NotBeNull();
    }

    [Fact]
    public async Task AttendanceRecord_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var r = MakeRecord(tidA, Guid.NewGuid(), Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.AttendanceRecords.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AttendanceRecordRepository(q).GetByIdAsync(r.Id, tidB)).Should().BeNull();
    }

    [Fact]
    public async Task AttendanceRecord_GetByStudentCourse_CrossTenant_ReturnsEmpty()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sid = Guid.NewGuid(); var cid = Guid.NewGuid();
        var r = MakeRecord(tidA, sid, cid);
        using (var ctx = MakeDb(db)) { ctx.AttendanceRecords.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AttendanceRecordRepository(q).GetByStudentCourseAsync(sid, cid, tidB)).Should().BeEmpty();
    }

    [Fact]
    public async Task AttendanceRecord_GetByStudentCourse_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var sid = Guid.NewGuid(); var cid = Guid.NewGuid();
        var r = MakeRecord(tid, sid, cid);
        using (var ctx = MakeDb(db)) { ctx.AttendanceRecords.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new AttendanceRecordRepository(q).GetByStudentCourseAsync(sid, cid, tid)).Should().HaveCount(1);
    }

    // ── CondonationRequest ────────────────────────────────────

    [Fact]
    public async Task Condonation_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var c = MakeCondonation(tidA, Guid.NewGuid());
        using (var ctx = MakeDb(db)) { ctx.CondonationRequests.Add(c); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new CondonationRequestRepository(q).GetByIdAsync(c.Id, tidB)).Should().BeNull();
    }

    [Fact]
    public async Task Condonation_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid(); var sid = Guid.NewGuid();
        var c = MakeCondonation(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.CondonationRequests.Add(c); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new CondonationRequestRepository(q).GetByStudentAsync(sid, tidB)).Should().BeEmpty();
    }

    [Fact]
    public async Task Condonation_GetByStudent_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid(); var sid = Guid.NewGuid();
        var c = MakeCondonation(tid, sid);
        using (var ctx = MakeDb(db)) { ctx.CondonationRequests.Add(c); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new CondonationRequestRepository(q).GetByStudentAsync(sid, tid)).Should().HaveCount(1);
    }
}
