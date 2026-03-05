using FluentAssertions;
using Identity.Application.Features.Tenants.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Moq;

namespace Identity.Tests.Application;

public sealed class ProvisionTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenants = new();

    private ProvisionTenantCommandHandler BuildHandler() =>
        new(_tenants.Object);

    private static ProvisionTenantCommand ValidCommand(string slug = "new-uni") => new(
        Name: "New University",
        Slug: slug,
        Tier: "Shared",
        Region: "default"
    );

    [Fact]
    public async Task Handle_NewSlug_ReturnsTenantId()
    {
        _tenants.Setup(r => r.FindBySlugAsync("new-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);
        _tenants.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.TenantId.Should().NotBeEmpty();
        result.Slug.Should().Be("new-uni");
        result.Status.Should().Be("Trial");
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsTenantAlreadyExistsException()
    {
        var existing = Tenant.Create("Existing Uni", "new-uni");
        _tenants.Setup(r => r.FindBySlugAsync("new-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

        var act = async () => await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<TenantAlreadyExistsException>()
                 .WithMessage("*new-uni*");
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ExceptionCodeIsDuplicateSlug()
    {
        var existing = Tenant.Create("Existing Uni", "new-uni");
        _tenants.Setup(r => r.FindBySlugAsync("new-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

        var act = async () => await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<TenantAlreadyExistsException>();
        ex.Which.Code.Should().Be("DUPLICATE_SLUG");
    }

    [Fact]
    public async Task Handle_InvalidTier_DefaultsToShared()
    {
        _tenants.Setup(r => r.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        Tenant? captured = null;
        _tenants.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback<Tenant, CancellationToken>((t, _) => captured = t)
                .Returns(Task.CompletedTask);

        await BuildHandler().Handle(
            new ProvisionTenantCommand("X", "x-uni", "InvalidTier", "default"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Tier.Should().Be(TenantTier.Shared);
    }

    [Fact]
    public async Task Handle_EnterpriseTier_SetsMaxUsers10000()
    {
        _tenants.Setup(r => r.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        Tenant? captured = null;
        _tenants.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback<Tenant, CancellationToken>((t, _) => captured = t)
                .Returns(Task.CompletedTask);

        await BuildHandler().Handle(
            new ProvisionTenantCommand("Big Uni", "big-uni", "Enterprise", "default"),
            CancellationToken.None);

        captured!.MaxUsers.Should().Be(10000);
        captured.Tier.Should().Be(TenantTier.Enterprise);
    }
}
