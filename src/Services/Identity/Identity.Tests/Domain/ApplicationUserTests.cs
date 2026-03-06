using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Events;

namespace Identity.Tests.Domain;

public sealed class ApplicationUserTests
{
    private static ApplicationUser ValidUser() => ApplicationUser.Create(
        Guid.NewGuid(), "john@uni.edu", "John", "Doe");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var user = ApplicationUser.Create(tenantId, "John@UNI.edu", "John", "Doe");

        user.TenantId.Should().Be(tenantId);
        user.Email.Should().Be("john@uni.edu");           // normalized lower
        user.NormalizedEmail.Should().Be("JOHN@UNI.EDU"); // normalized upper
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.IsActive.Should().BeTrue();
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        var act = () => ApplicationUser.Create(Guid.NewGuid(), "", "John", "Doe");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyFirstName_Throws()
    {
        var act = () => ApplicationUser.Create(Guid.NewGuid(), "j@uni.edu", "", "Doe");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyLastName_Throws()
    {
        var act = () => ApplicationUser.Create(Guid.NewGuid(), "j@uni.edu", "John", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RaisesUserRegisteredEvent()
    {
        var user = ValidUser();
        user.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "UserRegisteredEvent");
    }

    [Fact]
    public void Create_UserRegisteredEvent_CarriesCorrectTenantId()
    {
        var tenantId = Guid.NewGuid();
        var user = ApplicationUser.Create(tenantId, "j@uni.edu", "John", "Doe");
        var evt = user.DomainEvents.OfType<UserRegisteredEvent>().Single();
        evt.TenantId.Should().Be(tenantId);
    }

    // ── FullName ──────────────────────────────────────────────────────────────

    [Fact]
    public void FullName_ReturnsFirstAndLastCombined()
    {
        var user = ValidUser();
        user.FullName.Should().Be("John Doe");
    }

    // ── RecordLogin ───────────────────────────────────────────────────────────

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = ValidUser();
        user.LastLoginAt.Should().BeNull();
        user.RecordLogin();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = ValidUser();
        user.IsActive.Should().BeTrue();
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    // ── ClearDomainEvents ─────────────────────────────────────────────────────

    [Fact]
    public void ClearDomainEvents_EmptiesCollection()
    {
        var user = ValidUser();
        user.DomainEvents.Should().NotBeEmpty();
        user.ClearDomainEvents();
        user.DomainEvents.Should().BeEmpty();
    }
}