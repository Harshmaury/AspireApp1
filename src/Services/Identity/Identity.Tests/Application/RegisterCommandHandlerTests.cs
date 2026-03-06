using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Identity.Tests.Application;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();

    private RegisterCommandHandler BuildHandler() =>
        new(_users.Object, _tenants.Object);

    private static Tenant ValidTenant()
    {
        var t = Tenant.Create("Test Uni", "test-uni");
        return t;
    }

    private static RegisterCommand ValidCommand(string tenantSlug = "test-uni") => new(
        TenantSlug: tenantSlug,
        Email: "john@uni.edu",
        Password: "Password@123",
        FirstName: "John",
        LastName: "Doe"
    );

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var tenant = ValidTenant();
        _tenants.Setup(r => r.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(r => r.ExistsAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), "Password@123"))
              .ReturnsAsync(IdentityResult.Success);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.UserId.Should().NotBeNull().And.NotBe(Guid.Empty);
        result.Errors.Should().BeEmpty();
    }

    // ── Tenant not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TenantNotFound_ThrowsTenantNotFoundException()
    {
        _tenants.Setup(r => r.FindBySlugAsync("bad-slug", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        var act = async () => await BuildHandler().Handle(
            ValidCommand("bad-slug"), CancellationToken.None);

        await act.Should().ThrowAsync<TenantNotFoundException>();
    }

    // ── Duplicate email ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailureWithMessage()
    {
        var tenant = ValidTenant();
        _tenants.Setup(r => r.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(r => r.ExistsAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.Errors.Should().ContainSingle(e => e.Contains("already registered"));
    }

    // ── Identity failure ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_IdentityCreateFails_ReturnsFailureWithErrors()
    {
        var tenant = ValidTenant();
        _tenants.Setup(r => r.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(r => r.ExistsAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
              .ReturnsAsync(IdentityResult.Failed(
                  new IdentityError { Code = "WeakPassword", Description = "Password too weak." }));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Password too weak"));
    }

    // ── Domain event raised ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_UserRaisesRegisteredEvent()
    {
        var tenant = ValidTenant();
        ApplicationUser? capturedUser = null;

        _tenants.Setup(r => r.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(r => r.ExistsAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
              .Callback<ApplicationUser, string>((u, _) => capturedUser = u)
              .ReturnsAsync(IdentityResult.Success);

        await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        capturedUser.Should().NotBeNull();
        capturedUser!.DomainEvents.Should().ContainSingle(
            e => e.GetType().Name == "UserRegisteredEvent");
    }
}