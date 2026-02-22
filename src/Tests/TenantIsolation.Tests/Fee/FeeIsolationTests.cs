using Fee.Domain.Entities;
using Fee.Domain.Enums;
using Fee.Infrastructure.Persistence;
using Fee.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using TenantIsolation.Tests.Helpers;
using Xunit;

namespace TenantIsolation.Tests.Fee;

public sealed class FeeIsolationTests
{
    private static FeeDbContext MakeDb(string name) =>
        DbFactory.Create<FeeDbContext>(o => new FeeDbContext(o), name);

    private static FeeStructure MakeStructure(Guid tenantId) =>
        FeeStructure.Create(tenantId, Guid.NewGuid(), "2024-25", 1,
            50000m, 5000m, 2000m, 1000m, DateTime.UtcNow.AddDays(30));

    private static FeePayment MakePayment(Guid tenantId, Guid studentId) =>
        FeePayment.Create(tenantId, studentId, Guid.NewGuid(),
            50000m, PaymentMode.Online, $"RCP{Guid.NewGuid():N}"[..8]);

    private static Scholarship MakeScholarship(Guid tenantId, Guid studentId) =>
        Scholarship.Create(tenantId, studentId, "Merit Scholarship", 10000m, "2024-25");

    // ── FeeStructure ──────────────────────────────────────────

    [Fact]
    public async Task FeeStructure_GetById_OwnTenant_Returns()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var fs   = MakeStructure(tidA);
        using (var ctx = MakeDb(db)) { ctx.FeeStructures.Add(fs); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new FeeStructureRepository(qCtx).GetByIdAsync(fs.Id, tidA);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FeeStructure_GetById_CrossTenant_ReturnsNull()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var tidB = Guid.NewGuid();
        var fsA  = MakeStructure(tidA);
        using (var ctx = MakeDb(db)) { ctx.FeeStructures.Add(fsA); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new FeeStructureRepository(qCtx).GetByIdAsync(fsA.Id, tidB);
        result.Should().BeNull();
    }

    // ── FeePayment ────────────────────────────────────────────

    [Fact]
    public async Task FeePayment_GetByStudent_OwnTenant_Returns()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var p    = MakePayment(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.FeePayments.Add(p); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new FeePaymentRepository(qCtx).GetByStudentAsync(sid, tidA);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FeePayment_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var tidB = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var pA   = MakePayment(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.FeePayments.Add(pA); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new FeePaymentRepository(qCtx).GetByStudentAsync(sid, tidB);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FeePayment_GetById_CrossTenant_ReturnsNull()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var p    = MakePayment(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.FeePayments.Add(p); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new FeePaymentRepository(qCtx).GetByIdAsync(p.Id, Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── Scholarship ───────────────────────────────────────────

    [Fact]
    public async Task Scholarship_GetByStudent_CrossTenant_ReturnsEmpty()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var tidB = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var sc   = MakeScholarship(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.Scholarships.Add(sc); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new ScholarshipRepository(qCtx).GetByStudentAsync(sid, tidB);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Scholarship_GetById_CrossTenant_ReturnsNull()
    {
        var db   = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid();
        var sid  = Guid.NewGuid();
        var sc   = MakeScholarship(tidA, sid);
        using (var ctx = MakeDb(db)) { ctx.Scholarships.Add(sc); ctx.SaveChanges(); }

        using var qCtx = MakeDb(db);
        var result = await new ScholarshipRepository(qCtx).GetByIdAsync(sc.Id, Guid.NewGuid());
        result.Should().BeNull();
    }
}
