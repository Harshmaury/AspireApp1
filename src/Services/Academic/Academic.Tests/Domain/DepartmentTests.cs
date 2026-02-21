using Xunit;
using Academic.Domain.Entities;
using Academic.Domain.Enums;
using Academic.Domain.Exceptions;
using FluentAssertions;
namespace Academic.Tests.Domain;
public sealed class DepartmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsDepartment()
    {
        var dept = Department.Create(TenantId, "Computer Science", "CSE", 1972, "Top dept");
        dept.Should().NotBeNull();
        dept.Name.Should().Be("Computer Science");
        dept.Code.Should().Be("CSE");
        dept.Status.Should().Be(DepartmentStatus.Active);
        dept.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_EmptyName_ThrowsException()
    {
        var act = () => Department.Create(TenantId, "", "CSE", 1972);
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Create_EmptyCode_ThrowsException()
    {
        var act = () => Department.Create(TenantId, "Computer Science", "", 1972);
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Deactivate_ActiveDepartment_SetsInactive()
    {
        var dept = Department.Create(TenantId, "Computer Science", "CSE", 1972);
        dept.Deactivate();
        dept.Status.Should().Be(DepartmentStatus.Inactive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsException()
    {
        var dept = Department.Create(TenantId, "Computer Science", "CSE", 1972);
        dept.Deactivate();
        var act = () => dept.Deactivate();
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        var dept = Department.Create(TenantId, "Computer Science", "CSE", 1972);
        dept.DomainEvents.Should().HaveCount(1);
    }
}