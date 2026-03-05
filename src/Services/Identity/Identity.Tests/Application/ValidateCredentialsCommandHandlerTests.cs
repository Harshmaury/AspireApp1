using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Identity.Tests.Application;

public sealed class ValidateCredentialsCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();

    private ValidateCredentialsCommandHandler BuildHandler(bool writeAllowed = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REGION_WRITE_ALLOWED"] = writeAllowed.ToString().ToLower()
            })
            .Build();
        return new(_users.Object, _tenants.Object, config);
    }

    private static Tenant ActiveTenant() => Tenant.Create("Test Uni", "test-uni");
    private static ApplicationUser ActiveUser(Guid tenantId)
        => ApplicationUser.Create(tenantId, "john@uni.edu", "John", "Doe");

    private static ValidateCredentialsCommand ValidCommand() => new(
        TenantSlug: "test-uni",
        Email:      "john@uni.edu",
        Password:   "Pass@123"
    );

    // -- Happy path --------------------------------------------------------

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithUserTenantAndRoles()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(u => u.GetRolesAsync(user, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<string> { "Student" });
        _users.Setup(u => u.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.User.Should().BeSameAs(user);
        result.Tenant.Should().BeSameAs(tenant);
        result.Roles.Should().ContainSingle("Student");
        result.Error.Should().BeNull();
    }

    // -- Tenant guards -----------------------------------------------------

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsFailure()
    {
        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Tenant not found.");
    }

    [Fact]
    public async Task Handle_SuspendedTenant_ReturnsFailure()
    {
        var tenant = ActiveTenant();
        tenant.Activate();
        tenant.Suspend();

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Tenant account is not available.");
    }

    [Fact]
    public async Task Handle_CancelledTenant_ReturnsFailure()
    {
        var tenant = ActiveTenant();
        tenant.Cancel();

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Tenant account is not available.");
    }

    // -- User guards -------------------------------------------------------

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var tenant = ActiveTenant();

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync((ApplicationUser?)null);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsFailure()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);
        user.Deactivate();

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    // -- Lockout guards ----------------------------------------------------

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.Failed);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_LockedOutUser_ReturnsLockedOutMessage()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.LockedOut);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Account is temporarily locked. Try again later.");
    }

    // -- Region write guard ------------------------------------------------

    [Fact]
    public async Task Handle_PrimaryRegion_CallsRecordLogin()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(u => u.GetRolesAsync(user, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<string>());
        _users.Setup(u => u.UpdateAsync(user)).Returns(Task.CompletedTask);

        await BuildHandler(writeAllowed: true).Handle(ValidCommand(), CancellationToken.None);

        _users.Verify(u => u.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_SecondaryRegion_SkipsRecordLogin_ButSucceeds()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(u => u.GetRolesAsync(user, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<string>());

        var result = await BuildHandler(writeAllowed: false).Handle(ValidCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _users.Verify(u => u.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    // -- Roles in result ---------------------------------------------------

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAllRoles()
    {
        var tenant = ActiveTenant();
        var user   = ActiveUser(tenant.Id);
        var expectedRoles = new List<string> { "Faculty", "Admin" };

        _tenants.Setup(t => t.FindBySlugAsync("test-uni", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
        _users.Setup(u => u.FindByEmailAsync(tenant.Id, "john@uni.edu", It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordWithLockoutAsync(user, "Pass@123", It.IsAny<CancellationToken>()))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(u => u.GetRolesAsync(user, It.IsAny<CancellationToken>()))
              .ReturnsAsync(expectedRoles);
        _users.Setup(u => u.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Roles.Should().BeEquivalentTo(expectedRoles);
    }
}