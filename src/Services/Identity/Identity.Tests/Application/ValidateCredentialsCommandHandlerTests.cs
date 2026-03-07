// src/Services/Identity/Identity.Tests/Application/ValidateCredentialsCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Tests.Fakers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ValidateCredentialsCommandHandlerTests
{
    private readonly Mock<IUserRepository>   _users   = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<IAuditLogger>      _audit   = new();

    private ValidateCredentialsCommandHandler Sut(bool writeAllowed = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
                { ["REGION_WRITE_ALLOWED"] = writeAllowed.ToString() })
            .Build();

        return new(_users.Object, _tenants.Object, _audit.Object, accessor.Object, config);
    }

    private static ValidateCredentialsCommand Cmd(string slug = "acme") =>
        new(TenantSlug: slug, Email: "user@test.com", Password: "Password1");

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_return_success_with_valid_credentials()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(user);
        _users.Setup(x => x.CheckPasswordWithLockoutAsync(user, "Password1", default))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(x => x.GetRolesAsync(user, default)).ReturnsAsync(new List<string> { "Student" });
        _users.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeTrue();
        result.User.Should().Be(user);
        result.Roles.Should().Contain("Student");
    }

    // â”€â”€ Guard: tenant not found â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_fail_when_tenant_not_found()
    {
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync((Tenant?)null);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Invalid credentials");
    }

    // â”€â”€ Guard: suspended tenant â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_fail_when_tenant_is_suspended()
    {
        var tenant = TenantFaker.Suspended();
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeFalse();
    }

    // â”€â”€ Guard: wrong password â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_fail_when_password_is_wrong()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(user);
        _users.Setup(x => x.CheckPasswordWithLockoutAsync(user, "Password1", default))
              .ReturnsAsync(PasswordCheckResult.Failed);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Invalid credentials");
    }

    // â”€â”€ Guard: locked out â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_fail_when_account_is_locked_out()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(user);
        _users.Setup(x => x.CheckPasswordWithLockoutAsync(user, "Password1", default))
              .ReturnsAsync(PasswordCheckResult.LockedOut);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    // â”€â”€ Guard: inactive user â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_fail_when_user_is_inactive()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Inactive(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(user);

        var result = await Sut().Handle(Cmd(), default);

        result.Succeeded.Should().BeFalse();
    }

    // â”€â”€ Read-replica region: no UpdateAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_not_update_user_when_region_is_read_only()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(user);
        _users.Setup(x => x.CheckPasswordWithLockoutAsync(user, "Password1", default))
              .ReturnsAsync(PasswordCheckResult.Success);
        _users.Setup(x => x.GetRolesAsync(user, default)).ReturnsAsync(new List<string>());

        await Sut(writeAllowed: false).Handle(Cmd(), default);

        _users.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
