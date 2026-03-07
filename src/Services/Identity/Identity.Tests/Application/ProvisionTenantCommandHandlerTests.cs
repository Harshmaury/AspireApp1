// src/Services/Identity/Identity.Tests/Application/ProvisionTenantCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Tenants.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ProvisionTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenants = new();

    private ProvisionTenantCommandHandler Sut() =>
        new ProvisionTenantCommandHandler(_tenants.Object);

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_create_and_persist_tenant()
    {
        _tenants.Setup(x => x.FindBySlugAsync("uni-a", default))
                .ReturnsAsync((Tenant?)null);
        _tenants.Setup(x => x.AddAsync(It.IsAny<Tenant>(), default))
                .Returns(Task.CompletedTask);

        var result = await Sut().Handle(
            new ProvisionTenantCommand("Uni A", "uni-a", "Shared", "india"), default);

        result.TenantId.Should().NotBeEmpty();
        result.Slug.Should().Be("uni-a");
        _tenants.Verify(x => x.AddAsync(It.IsAny<Tenant>(), default), Times.Once);
    }

    [Theory]
    [InlineData("Shared",     TenantTier.Shared)]
    [InlineData("Dedicated",  TenantTier.Dedicated)]
    [InlineData("Enterprise", TenantTier.Enterprise)]
    [InlineData("unknown",    TenantTier.Shared)] // falls back to Shared
    public async Task Handle_should_map_tier_string_correctly(string tierStr, TenantTier expected)
    {
        _tenants.Setup(x => x.FindBySlugAsync(It.IsAny<string>(), default))
                .ReturnsAsync((Tenant?)null);

        Tenant? saved = null;
        _tenants.Setup(x => x.AddAsync(It.IsAny<Tenant>(), default))
                .Callback<Tenant, CancellationToken>((t, _) => saved = t)
                .Returns(Task.CompletedTask);

        await Sut().Handle(
            new ProvisionTenantCommand("X", "x", tierStr, "default"), default);

        saved!.Tier.Should().Be(expected);
    }

    // â”€â”€ Guard: duplicate slug â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_TenantAlreadyExistsException_for_duplicate_slug()
    {
        var existing = Tenant.Create("Existing", "dup-slug");
        _tenants.Setup(x => x.FindBySlugAsync("dup-slug", default))
                .ReturnsAsync(existing);

        var act = () => Sut().Handle(
            new ProvisionTenantCommand("New", "dup-slug", "Shared", "default"), default);

        await act.Should().ThrowAsync<TenantAlreadyExistsException>();
    }
}
