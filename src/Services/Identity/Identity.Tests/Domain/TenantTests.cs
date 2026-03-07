// src/Services/Identity/Identity.Tests/Domain/TenantTests.cs
using FluentAssertions;
using Identity.Domain.Common;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Xunit;

namespace Identity.Tests.Domain;

public sealed class TenantTests
{
    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Create_should_raise_TenantProvisionedEvent()
    {
        var tenant = Tenant.Create("Uni A", "uni-a", TenantTier.Shared, "india");

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantProvisionedEvent>();
    }

    [Fact]
    public void Create_should_set_MaxUsers_based_on_tier()
    {
        Tenant.Create("A", "a", TenantTier.Shared).MaxUsers.Should().Be(100);
        Tenant.Create("B", "b", TenantTier.Dedicated).MaxUsers.Should().Be(1000);
        Tenant.Create("C", "c", TenantTier.Enterprise).MaxUsers.Should().Be(10000);
    }

    [Fact]
    public void Create_should_normalise_slug_to_lowercase()
    {
        var tenant = Tenant.Create("X", "My-Slug");
        tenant.Slug.Should().Be("my-slug");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_should_throw_for_blank_name(string name)
    {
        var act = () => Tenant.Create(name, "slug");
        act.Should().Throw<ArgumentException>();
    }

    // â”€â”€ Suspend â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Suspend_should_raise_TenantSuspendedEvent()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.Activate();
        tenant.ClearDomainEvents();

        tenant.Suspend();

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantSuspendedEvent>();
    }

    [Fact]
    public void Suspend_should_be_idempotent()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.Activate();
        tenant.Suspend();
        tenant.ClearDomainEvents();

        tenant.Suspend(); // second call

        tenant.DomainEvents.Should().BeEmpty();
        tenant.SubscriptionStatus.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public void Suspend_should_throw_when_tenant_is_cancelled()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.Cancel();

        var act = () => tenant.Suspend();
        act.Should().Throw<InvalidOperationException>();
    }

    // â”€â”€ Upgrade â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Upgrade_should_raise_TenantUpgradedEvent()
    {
        var tenant = Tenant.Create("X", "x", TenantTier.Shared);
        tenant.Activate();
        tenant.ClearDomainEvents();

        tenant.Upgrade(TenantTier.Dedicated);

        var evt = tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantUpgradedEvent>().Subject;

        evt.OldTier.Should().Be(TenantTier.Shared);
        evt.NewTier.Should().Be(TenantTier.Dedicated);
    }

    [Fact]
    public void Upgrade_should_update_MaxUsers()
    {
        var tenant = Tenant.Create("X", "x", TenantTier.Shared);
        tenant.Activate();

        tenant.Upgrade(TenantTier.Enterprise);

        tenant.MaxUsers.Should().Be(10000);
    }

    [Fact]
    public void Upgrade_should_throw_when_cancelled()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.Cancel();

        var act = () => tenant.Upgrade(TenantTier.Dedicated);
        act.Should().Throw<InvalidOperationException>();
    }

    // â”€â”€ IAggregateRoot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Tenant_should_implement_IAggregateRoot()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.Should().BeAssignableTo<IAggregateRoot>();
    }

    [Fact]
    public void ClearDomainEvents_should_empty_the_collection()
    {
        var tenant = Tenant.Create("X", "x");
        tenant.DomainEvents.Should().NotBeEmpty();

        tenant.ClearDomainEvents();

        tenant.DomainEvents.Should().BeEmpty();
    }
}
