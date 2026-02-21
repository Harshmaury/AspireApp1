using Identity.Domain.Entities;
using FluentAssertions;

namespace Identity.Tests.Domain;

public sealed class ApplicationUserTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsUser()
    {
        var user = ApplicationUser.Create(TenantId, "john@uni.edu", "John", "Doe");
        user.Should().NotBeNull();
        user.Email.Should().Be("john@uni.edu");
        user.TenantId.Should().Be(TenantId);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmailNormalisedToLower()
    {
        var user = ApplicationUser.Create(TenantId, "JOHN@UNI.EDU", "John", "Doe");
        user.Email.Should().Be("john@uni.edu");
        user.UserName.Should().Be("john@uni.edu");
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        var act = () => ApplicationUser.Create(TenantId, "", "John", "Doe");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyFirstName_Throws()
    {
        var act = () => ApplicationUser.Create(TenantId, "john@uni.edu", "", "Doe");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyLastName_Throws()
    {
        var act = () => ApplicationUser.Create(TenantId, "john@uni.edu", "John", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RaisesUserRegisteredEvent()
    {
        var user = ApplicationUser.Create(TenantId, "john@uni.edu", "John", "Doe");
        user.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "UserRegisteredEvent");
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = ApplicationUser.Create(TenantId, "john@uni.edu", "John", "Doe");
        user.RecordLogin();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = ApplicationUser.Create(TenantId, "john@uni.edu", "John", "Doe");
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void FullName_ReturnsFirstAndLastName()
    {
        var user = ApplicationUser.Create(TenantId, "john@uni.edu", "John", "Doe");
        user.FullName.Should().Be("John Doe");
    }
}
