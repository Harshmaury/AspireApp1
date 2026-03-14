// src/Services/Identity/Identity.Tests/Application/ForgotPasswordCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Tests.Fakers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository>              _users   = new();
    private readonly Mock<ITenantRepository>            _tenants = new();
    private readonly Mock<IVerificationTokenRepository> _tokens  = new();
    private readonly Mock<IAuditLogger>                 _audit   = new();
    private readonly IConfiguration                     _config;

    public ForgotPasswordCommandHandlerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://app.ums.edu"
            })
            .Build();
    }

    private ForgotPasswordCommandHandler Sut() =>
        new(_users.Object, _tenants.Object, _tokens.Object, _audit.Object, _config);

    // ── Anti-enumeration: unknown tenant ──────────────────────────────────────

    [Fact]
    public async Task Handle_should_return_success_when_tenant_not_found()
    {
        _tenants.Setup(x => x.FindBySlugAsync("ghost", default))
                .ReturnsAsync((Tenant?)null);

        var result = await Sut().Handle(new ForgotPasswordCommand("ghost", "x@x.com"), default);

        result.Succeeded.Should().BeTrue();
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Never);
    }

    // ── Anti-enumeration: unknown user ────────────────────────────────────────

    [Fact]
    public async Task Handle_should_return_success_when_user_not_found()
    {
        var tenant = TenantFaker.Active();
        _tenants.Setup(x => x.FindBySlugAsync(tenant.Slug, default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, "x@x.com", default))
              .ReturnsAsync((ApplicationUser?)null);

        var result = await Sut().Handle(
            new ForgotPasswordCommand(tenant.Slug, "x@x.com"), default);

        result.Succeeded.Should().BeTrue();
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Never);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_should_create_token_and_return_success_for_valid_user()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync(tenant.Slug, default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, user.Email!, default)).ReturnsAsync(user);
        _tokens.Setup(x => x.InvalidateByUserAsync(user.Id, TokenPurpose.PasswordReset, default))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.CreateAsync(It.IsAny<VerificationToken>(), default))
               .Returns(Task.CompletedTask);
        _users.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await Sut().Handle(
            new ForgotPasswordCommand(tenant.Slug, user.Email!), default);

        result.Succeeded.Should().BeTrue();
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Once);
    }

    // ── P0-2: domain event must be raised ─────────────────────────────────────

    [Fact]
    public async Task Handle_should_publish_PasswordResetRequestedEvent()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync(tenant.Slug, default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, user.Email!, default)).ReturnsAsync(user);
        _tokens.Setup(x => x.InvalidateByUserAsync(user.Id, TokenPurpose.PasswordReset, default))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.CreateAsync(It.IsAny<VerificationToken>(), default))
               .Returns(Task.CompletedTask);
        _users.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await Sut().Handle(
            new ForgotPasswordCommand(tenant.Slug, user.Email!), default);

        result.Succeeded.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle(e =>
            e is Identity.Domain.Events.PasswordResetRequestedEvent);
    }
}
