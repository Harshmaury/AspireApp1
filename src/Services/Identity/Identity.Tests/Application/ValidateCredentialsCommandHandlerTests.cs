// src/Services/Identity/Identity.Tests/Application/ValidateCredentialsCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Identity.Tests.Application;

public sealed class ValidateCredentialsCommandHandlerTests
{
    private readonly Mock<IUserRepository>       _users   = new();
    private readonly Mock<ITenantRepository>     _tenants = new();
    private readonly Mock<IAuditLogger>          _audit   = new();
    private readonly Mock<IHttpContextAccessor>  _http    = new();
    private readonly Mock<IConfiguration>        _config  = new();

    private ValidateCredentialsCommandHandler CreateHandler() =>
        new(_users.Object, _tenants.Object, _audit.Object, _http.Object, _config.Object);

    [Fact]
    public async Task Returns_failure_when_tenant_not_found()
    {
        _tenants
            .Setup(t => t.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await CreateHandler()
            .Handle(new ValidateCredentialsCommand("missing", "u@t.com", "Pass1"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Returns_failure_when_user_not_found()
    {
        var tenant = Tenant.Create("Test", "test-tenant", TenantTier.Shared, "default");

        _tenants
            .Setup(t => t.FindBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _users
            .Setup(u => u.FindByEmailAsync(tenant.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateHandler()
            .Handle(new ValidateCredentialsCommand("test-tenant", "missing@t.com", "Pass1"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Returns_failure_when_password_wrong()
    {
        var tenant = Tenant.Create("Test", "test-tenant", TenantTier.Shared, "default");
        var user   = ApplicationUser.Create(tenant.Id, "user@test.com", "First", "Last");

        _tenants
            .Setup(t => t.FindBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _users
            .Setup(u => u.FindByEmailAsync(tenant.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _users
            .Setup(u => u.CheckPasswordWithLockoutAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordCheckResult.Failed);

        var result = await CreateHandler()
            .Handle(new ValidateCredentialsCommand("test-tenant", "user@test.com", "Wrong1"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Returns_failure_when_account_locked_out()
    {
        var tenant = Tenant.Create("Test", "test-tenant", TenantTier.Shared, "default");
        var user   = ApplicationUser.Create(tenant.Id, "user@test.com", "First", "Last");

        _tenants
            .Setup(t => t.FindBySlugAsync("test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _users
            .Setup(u => u.FindByEmailAsync(tenant.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _users
            .Setup(u => u.CheckPasswordWithLockoutAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordCheckResult.LockedOut);

        var result = await CreateHandler()
            .Handle(new ValidateCredentialsCommand("test-tenant", "user@test.com", "Pass1"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Account is temporarily locked. Try again later.");
    }
}
