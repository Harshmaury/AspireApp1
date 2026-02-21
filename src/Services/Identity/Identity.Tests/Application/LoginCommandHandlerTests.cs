using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Identity.Tests.Application;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<ITokenService> _tokens = new();

    private LoginCommandHandler BuildHandler() =>
        new(_users.Object, _tenants.Object, _tokens.Object);

    private static readonly Tenant ValidTenant = Tenant.Create("Test Uni", "test-uni");
    private static readonly ApplicationUser ValidUser =
        ApplicationUser.Create(Guid.NewGuid(), "john@uni.edu", "John", "Doe");

    private static LoginCommand ValidCommand() => new(
        TenantSlug: "test-uni",
        Email: "john@uni.edu",
        Password: "Pass@123"
    );

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(ValidUser);
        _users.Setup(u => u.CheckPasswordAsync(ValidUser, "Pass@123"))
              .ReturnsAsync(true);
        _users.Setup(u => u.UpdateAsync(ValidUser)).Returns(Task.CompletedTask);
        _tokens.Setup(t => t.GenerateTokenAsync(ValidUser, ValidTenant, It.IsAny<CancellationToken>()))
               .ReturnsAsync("jwt-token");

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.AccessToken.Should().Be("jwt-token");
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
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync((ApplicationUser?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidTenant);
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<Guid>(), "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(ValidUser);
        _users.Setup(u => u.CheckPasswordAsync(ValidUser, "Pass@123"))
              .ReturnsAsync(false);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }
}

