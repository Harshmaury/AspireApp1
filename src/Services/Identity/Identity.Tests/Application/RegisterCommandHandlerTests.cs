using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using Moq;

namespace Identity.Tests.Application;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();

    private RegisterCommandHandler BuildHandler() =>
        new(_users.Object, _tenants.Object);

    private static readonly Tenant ValidTenant = Tenant.Create("Test Uni", "test-uni");

    private static RegisterCommand ValidCommand() => new(
        TenantSlug: "test-uni",
        Email: "john@uni.edu",
        Password: "Pass@123",
        FirstName: "John",
        LastName: "Doe"
    );

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Pass@123"))
              .ReturnsAsync(IdentityResult.Success);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.UserId.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TenantNotFound_ThrowsTenantNotFoundException()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        var act = async () => await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<TenantNotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_IdentityFailure_ReturnsFailure()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Pass@123"))
              .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Password too weak.");
    }
}

