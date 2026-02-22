using Examination.Domain.Entities;
using Examination.Domain.Enums;
using Examination.Infrastructure.Persistence;
using Examination.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using TenantIsolation.Tests.Helpers;
using Xunit;

namespace TenantIsolation.Tests.Examination;

public sealed class ExaminationIsolationTests
{
    private static ExaminationDbContext MakeDb(string name) =>
        DbFactory.Create<ExaminationDbContext>(o => new ExaminationDbContext(o), name);

    private static ExamSchedule MakeSchedule(Guid tenantId) =>
        ExamSchedule.Create(tenantId, Guid.NewGuid(),
            "2024-25", 1, ExamType.MidSem,
            DateTime.UtcNow.AddDays(10), 180, "Hall A", 100, 40);

    private static MarksEntry MakeMarks(Guid tenantId, Guid scheduleId, Guid studentId) =>
        MarksEntry.Create(tenantId, studentId, scheduleId, Guid.NewGuid(), 75m, 100, false, Guid.NewGuid());

    private static ResultCard MakeResult(Guid tenantId, Guid studentId) =>
        ResultCard.Create(tenantId, studentId, "2024-25", 1, 8.5m, 8.2m, 24, 24);

    // ── ExamSchedule ──────────────────────────────────────────

    [Fact]
    public async Task ExamSchedule_GetById_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var s = MakeSchedule(tid);
        using (var ctx = MakeDb(db)) { ctx.ExamSchedules.Add(s); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ExamScheduleRepository(q).GetByIdAsync(s.Id, tid)).Should().NotBeNull();
    }

    [Fact]
    public async Task ExamSchedule_GetById_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sA = MakeSchedule(tidA); var sB = MakeSchedule(tidB);
        using (var ctx = MakeDb(db)) { ctx.ExamSchedules.AddRange(sA, sB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ExamScheduleRepository(q).GetByIdAsync(sA.Id, tidB)).Should().BeNull();
    }

    // ── MarksEntry ────────────────────────────────────────────

    [Fact]
    public async Task MarksEntry_GetByStudent_OwnTenant_Returns()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid(); var sid = Guid.NewGuid();
        var m = MakeMarks(tid, Guid.NewGuid(), sid);
        using (var ctx = MakeDb(db)) { ctx.MarksEntries.Add(m); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new MarksEntryRepository(q).GetByStudentAsync(sid, tid)).Should().HaveCount(1);
    }

    [Fact]
    public async Task MarksEntry_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid(); var sid = Guid.NewGuid();
        var m = MakeMarks(tidA, Guid.NewGuid(), sid);
        using (var ctx = MakeDb(db)) { ctx.MarksEntries.Add(m); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new MarksEntryRepository(q).GetByStudentAsync(sid, tidB)).Should().BeEmpty();
    }

    // ── ResultCard ────────────────────────────────────────────

    [Fact]
    public async Task ResultCard_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid(); var sid = Guid.NewGuid();
        var r = MakeResult(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.ResultCards.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ResultCardRepository(q).GetByStudentAsync(sid, tidB)).Should().BeEmpty();
    }

    [Fact]
    public async Task ResultCard_GetByStudentSemester_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid(); var sid = Guid.NewGuid();
        var r = MakeResult(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.ResultCards.Add(r); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new ResultCardRepository(q).GetByStudentSemesterAsync(sid, "2024-25", 1, tidB)).Should().BeNull();
    }
}
