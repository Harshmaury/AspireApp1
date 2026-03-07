// src/Services/Identity/Identity.Tests/Application/VerifyEmailCommandHandlerTests.cs
using FluentAssertions;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Tests.Fakers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IVerificationTokenRepository>   _tokens = new();
    private readonly Mock<UserManager<ApplicationUser>>   _userMgr;
    private readonly Mock<IAuditLogger>                   _audit  = new();
    private readonly IHttpContextAccessor                 _http;

    public VerifyEmailCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userMgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var httpContext = new DefaultHttpContext();
        var accessor    = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        _http = accessor.Object;
    }

    private VerifyEmailCommandHandler Sut() =>
        new(_tokens.Object, _userMgr.Object, _audit.Object, _http);

    // â”€â”€ P0-3: MarkUsed then SaveAsync â€” should NOT call CreateAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_not_call_CreateAsync_on_existing_token()
    {
        var user  = ApplicationUserFaker.Unverified();
        var (token, raw) = VerificationToken.Create(user.Id, user.TenantId,
            TokenPurpose.EmailVerification);

        _tokens.Setup(x => x.FindByHashAsync(
            VerificationToken.ComputeHash(raw), TokenPurpose.EmailVerification, default))
            .ReturnsAsync(token);

        _userMgr.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userMgr.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await Sut().Handle(new VerifyEmailCommand(raw), default);

        // MUST be called to persist MarkUsed â€” but via UpdateAsync(token), NOT CreateAsync
        _tokens.Verify(x => x.CreateAsync(It.IsAny<VerificationToken>(), default), Times.Never);
    }

    // â”€â”€ Guard: invalid token â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_InvalidVerificationTokenException_for_unknown_token()
    {
        _tokens.Setup(x => x.FindByHashAsync(It.IsAny<string>(),
            TokenPurpose.EmailVerification, default))
            .ReturnsAsync((VerificationToken?)null);

        var act = () => Sut().Handle(new VerifyEmailCommand("bad-token"), default);

        await act.Should().ThrowAsync<InvalidVerificationTokenException>();
    }

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_confirm_email_and_return_success()
    {
        var user  = ApplicationUserFaker.Unverified();
        var (token, raw) = VerificationToken.Create(user.Id, user.TenantId,
            TokenPurpose.EmailVerification);

        _tokens.Setup(x => x.FindByHashAsync(
            VerificationToken.ComputeHash(raw), TokenPurpose.EmailVerification, default))
            .ReturnsAsync(token);

        _userMgr.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userMgr.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await Sut().Handle(new VerifyEmailCommand(raw), default);

        result.Succeeded.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
    }
}
