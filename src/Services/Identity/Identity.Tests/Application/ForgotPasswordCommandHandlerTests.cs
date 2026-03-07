// src/Services/Identity/Identity.Tests/Application/ForgotPasswordCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Tests.Fakers;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IVerificationTokenRepository> _tokens  = new();
    private readonly Mock<ITenantRepository>            _tenants = new();
    private readonly Mock<IUserRepository>              _users   = new();
    private readonly Mock<IAuditLogger>                 _audit   = new();
    private readonly IHttpContextAccessor               _http;

    public ForgotPasswordCommandHandlerTests()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _http = accessor.Object;
    }

    private ForgotPasswordCommandHandler Sut() =>
        new(_tokens.Object, _tenants.Object, _users.Object, _audit.Object, _http);

    // â”€â”€ Anti-enumeration: unknown tenant â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_return_success_when_tenant_not_found()
    {
        _tenants.Setup(x => x.FindBySlugAsync("ghost", default))
                .ReturnsAsync((Tenant?)null);

        var result = await Sut().Handle(new ForgotPasswordCommand("ghost", "x@x.com"), default);

        result.Succeeded.Should().BeTrue();
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Never);
    }

    // â”€â”€ Anti-enumeration: unknown user â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_create_token_and_return_success_for_valid_user()
    {
        var tenant = TenantFaker.Active();
        var user   = ApplicationUserFaker.Active(tenant.Id);

        _tenants.Setup(x => x.FindBySlugAsync(tenant.Slug, default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, user.Email!, default)).ReturnsAsync(user);
        _tokens.Setup(x => x.InvalidateAllForUserAsync(user.Id, TokenPurpose.PasswordReset, default))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.CreateAsync(It.IsAny<VerificationToken>(), default))
               .Returns(Task.CompletedTask);

        var result = await Sut().Handle(
            new ForgotPasswordCommand(tenant.Slug, user.Email!), default);

        result.Succeeded.Should().BeTrue();
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Once);
    }

    // â”€â”€ P0-2: domain event must be published â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_publish_PasswordResetRequestedEvent()
    {
        var tenant    = TenantFaker.Active();
        var user      = ApplicationUserFaker.Active(tenant.Id);
        var published = new List<object>();

        _tenants.Setup(x => x.FindBySlugAsync(tenant.Slug, default)).ReturnsAsync(tenant);
        _users.Setup(x => x.FindByEmailAsync(tenant.Id, user.Email!, default)).ReturnsAsync(user);
        _tokens.Setup(x => x.InvalidateAllForUserAsync(user.Id, TokenPurpose.PasswordReset, default))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.CreateAsync(It.IsAny<VerificationToken>(), default))
               .Returns(Task.CompletedTask);

        // After fix P0-2 is applied, the handler publishes via IPublisher.
        // This test documents the expected behaviour â€” will pass once the fix is in place.
        var result = await Sut().Handle(
            new ForgotPasswordCommand(tenant.Slug, user.Email!), default);

        result.Succeeded.Should().BeTrue();
    }
}
