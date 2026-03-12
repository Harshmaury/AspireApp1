// src/Services/Identity/Identity.Tests/Domain/ApplicationUserTests.cs
using FluentAssertions;
using UMS.SharedKernel.Domain;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Xunit;

namespace Identity.Tests.Domain;

public sealed class ApplicationUserTests
{
    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Create_should_raise_UserRegisteredEvent()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "First", "Last");

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public void Create_should_normalise_email_to_lowercase()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "A@B.COM", "F", "L");
        user.Email.Should().Be("a@b.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_should_throw_for_blank_email(string email)
    {
        var act = () => ApplicationUser.Create(Guid.NewGuid(), email, "F", "L");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_should_set_IsActive_true()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "F", "L");
        user.IsActive.Should().BeTrue();
    }

    // â”€â”€ Deactivate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Deactivate_should_set_IsActive_false()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "F", "L");
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_should_raise_UserDeactivatedEvent()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "F", "L");
        user.ClearDomainEvents();

        user.Deactivate();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserDeactivatedEvent>();
    }

    // â”€â”€ IAggregateRoot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void ApplicationUser_should_implement_IAggregateRoot()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "F", "L");
        user.Should().BeAssignableTo<IAggregateRoot>();
    }

    // â”€â”€ FullName â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void FullName_should_concatenate_first_and_last()
    {
        var user = ApplicationUser.Create(Guid.NewGuid(), "a@b.com", "Jane", "Doe");
        user.FullName.Should().Be("Jane Doe");
    }
}
