using FluentAssertions;
using Student.Domain.Entities;
using Student.Infrastructure.Persistence;
using Student.Infrastructure.Persistence.Repositories;
using TenantIsolation.Tests.Helpers;
using Xunit;

namespace TenantIsolation.Tests.Student;

public sealed class StudentIsolationTests
{
    private static StudentDbContext MakeDb(string name) =>
        DbFactory.Create<StudentDbContext>(o => new StudentDbContext(o), name);

    private static StudentAggregate MakeStudent(Guid tenantId) =>
        StudentAggregate.Create(tenantId, Guid.NewGuid(),
            "First", "Last", $"s{Guid.NewGuid():N}@uni.edu");

    [Fact]
    public async Task GetByIdAsync_OwnTenant_ReturnsRecord()
    {
        var db = Guid.NewGuid().ToString(); var tid = Guid.NewGuid();
        var s = MakeStudent(tid);
        using (var ctx = MakeDb(db)) { ctx.Students.Add(s); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new StudentRepository(q).GetByIdAsync(s.Id, tid)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sA = MakeStudent(tidA); var sB = MakeStudent(tidB);
        using (var ctx = MakeDb(db)) { ctx.Students.AddRange(sA, sB); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new StudentRepository(q).GetByIdAsync(sA.Id, tidB)).Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_CrossTenant_ReturnsNull()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sA = MakeStudent(tidA);
        using (var ctx = MakeDb(db)) { ctx.Students.Add(sA); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new StudentRepository(q).GetByUserIdAsync(sA.UserId, tidB)).Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_CrossTenant_ReturnsFalse()
    {
        var db = Guid.NewGuid().ToString();
        var tidA = Guid.NewGuid(); var tidB = Guid.NewGuid();
        var sA = MakeStudent(tidA);
        using (var ctx = MakeDb(db)) { ctx.Students.Add(sA); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new StudentRepository(q).ExistsAsync(sA.UserId, tidB)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_OwnTenant_ReturnsTrue()
    {
        var db = Guid.NewGuid().ToString(); var tidA = Guid.NewGuid();
        var sA = MakeStudent(tidA);
        using (var ctx = MakeDb(db)) { ctx.Students.Add(sA); ctx.SaveChanges(); }
        using var q = MakeDb(db);
        (await new StudentRepository(q).ExistsAsync(sA.UserId, tidA)).Should().BeTrue();
    }
}
