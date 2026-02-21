using Xunit;
using Academic.Domain.Entities;
using Academic.Domain.Enums;
using Academic.Domain.Exceptions;
using FluentAssertions;
namespace Academic.Tests.Domain;
public sealed class ProgrammeTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsProgramme()
    {
        var p = Programme.Create(TenantId, DeptId, "B.Tech CSE", "BTECH-CSE", "BTech", 4, 160, 60);
        p.Should().NotBeNull();
        p.Code.Should().Be("BTECH-CSE");
        p.Status.Should().Be(ProgramStatus.Draft);
    }

    [Fact]
    public void Create_InvalidDuration_ThrowsException()
    {
        var act = () => Programme.Create(TenantId, DeptId, "B.Tech CSE", "BTECH-CSE", "BTech", 0, 160, 60);
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Activate_DraftProgramme_SetsActive()
    {
        var p = Programme.Create(TenantId, DeptId, "B.Tech CSE", "BTECH-CSE", "BTech", 4, 160, 60);
        p.Activate();
        p.Status.Should().Be(ProgramStatus.Active);
    }

    [Fact]
    public void Activate_RetiredProgramme_ThrowsException()
    {
        var p = Programme.Create(TenantId, DeptId, "B.Tech CSE", "BTECH-CSE", "BTech", 4, 160, 60);
        p.Activate();
        p.Retire();
        var act = () => p.Activate();
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Activate_RaisesProgrammeActivatedEvent()
    {
        var p = Programme.Create(TenantId, DeptId, "B.Tech CSE", "BTECH-CSE", "BTech", 4, 160, 60);
        p.Activate();
        p.DomainEvents.Should().Contain(e => e.GetType().Name == "ProgrammeActivatedEvent");
    }
}