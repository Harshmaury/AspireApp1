// src/Services/Identity/Identity.Tests/Application/RegisterCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Tests.Fakers;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository>   _users   = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<IAuditLogger>      _audit   = new();

    private RegisterCommandHandler Sut() =>
        new(_users.Object, _tenants.Object, _audit.Object);

    private static RegisterCommand ValidCmd(string slug = "acme") => new(
        TenantSlug: slug,
        Email:      "user@test.com",
        Password:   "Password1",
        FirstName:  "John",
        LastName:   "Doe");

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_return_success_when_registration_is_valid()
    {
        var tenant = TenantFaker.Active(selfReg: true);
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.CountByTenantAsync(tenant.Id, default)).ReturnsAsync(0);
        _users.Setup(x => x.ExistsAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(false);
        _users.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Password1"))
              .ReturnsAsync(IdentityResult.Success);

        var result = await Sut().Handle(ValidCmd(), default);

        result.Succeeded.Should().BeTrue();
        result.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_should_write_audit_log_on_success()
    {
        var tenant = TenantFaker.Active(selfReg: true);
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.CountByTenantAsync(tenant.Id, default)).ReturnsAsync(0);
        _users.Setup(x => x.ExistsAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(false);
        _users.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Password1"))
              .ReturnsAsync(IdentityResult.Success);

        await Sut().Handle(ValidCmd(), default);

        _audit.Verify(x => x.LogAsync(
            It.Is<AuditLog>(a => a.Action == AuditActions.Register && a.Succeeded),
            default), Times.Once);
    }

    // â”€â”€ Guard: tenant not found â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_TenantNotFoundException_when_tenant_missing()
    {
        _tenants.Setup(x => x.FindBySlugAsync("unknown", default)).ReturnsAsync((Tenant?)null);

        var act = () => Sut().Handle(ValidCmd("unknown"), default);

        await act.Should().ThrowAsync<TenantNotFoundException>();
    }

    // â”€â”€ Guard: self-registration disabled â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_SelfRegistrationDisabledException_when_flag_off()
    {
        var tenant = TenantFaker.WithNoSelfRegistration();
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);

        var act = () => Sut().Handle(ValidCmd(), default);

        await act.Should().ThrowAsync<SelfRegistrationDisabledException>();
    }

    // â”€â”€ Guard: user limit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_TenantUserLimitExceededException_when_at_cap()
    {
        var tenant = TenantFaker.Active(selfReg: true);
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.CountByTenantAsync(tenant.Id, default))
              .ReturnsAsync(tenant.MaxUsers);

        var act = () => Sut().Handle(ValidCmd(), default);

        await act.Should().ThrowAsync<TenantUserLimitExceededException>();
    }

    // â”€â”€ Guard: duplicate email â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_UserAlreadyExistsException_for_duplicate_email()
    {
        var tenant = TenantFaker.Active(selfReg: true);
        _tenants.Setup(x => x.FindBySlugAsync("acme", default)).ReturnsAsync(tenant);
        _users.Setup(x => x.CountByTenantAsync(tenant.Id, default)).ReturnsAsync(0);
        _users.Setup(x => x.ExistsAsync(tenant.Id, "user@test.com", default)).ReturnsAsync(true);

        var act = () => Sut().Handle(ValidCmd(), default);

        await act.Should().ThrowAsync<UserAlreadyExistsException>();
    }
}
