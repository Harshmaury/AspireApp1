// src/Services/Identity/Identity.Tests/Application/ResetPasswordCommandHandlerTests.cs
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

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IVerificationTokenRepository>   _tokens = new();
    private readonly Mock<UserManager<ApplicationUser>>   _userMgr;
    private readonly Mock<IAuditLogger>                   _audit  = new();
    private readonly IHttpContextAccessor                 _http;

    public ResetPasswordCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userMgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _http = accessor.Object;
    }

    private ResetPasswordCommandHandler Sut() =>
        new(_tokens.Object, _userMgr.Object, _audit.Object, _http);

    // â”€â”€ Guard: invalid token â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_throw_InvalidVerificationTokenException_for_unknown_token()
    {
        _tokens.Setup(x => x.FindByHashAsync(It.IsAny<string>(),
            TokenPurpose.PasswordReset, default))
            .ReturnsAsync((VerificationToken?)null);

        var act = () => Sut().Handle(new ResetPasswordCommand("bad", "NewPass1"), default);

        await act.Should().ThrowAsync<InvalidVerificationTokenException>();
    }

    // â”€â”€ Happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_reset_password_and_mark_token_used()
    {
        var user = ApplicationUserFaker.Active();
        var (token, raw) = VerificationToken.Create(user.Id, user.TenantId,
            TokenPurpose.PasswordReset);

        _tokens.Setup(x => x.FindByHashAsync(
            VerificationToken.ComputeHash(raw), TokenPurpose.PasswordReset, default))
            .ReturnsAsync(token);

        _userMgr.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userMgr.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("identity-token");
        _userMgr.Setup(x => x.ResetPasswordAsync(user, "identity-token", "NewPass1"))
                .ReturnsAsync(IdentityResult.Success);
        _tokens.Setup(x => x.InvalidateByUserAsync(user.Id, TokenPurpose.PasswordReset, default))
               .Returns(Task.CompletedTask);

        var result = await Sut().Handle(new ResetPasswordCommand(raw, "NewPass1"), default);

        result.Succeeded.Should().BeTrue();
        token.IsUsed.Should().BeTrue();
    }

    // â”€â”€ Guard: Identity password failure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Handle_should_return_failure_when_Identity_rejects_password()
    {
        var user = ApplicationUserFaker.Active();
        var (token, raw) = VerificationToken.Create(user.Id, user.TenantId,
            TokenPurpose.PasswordReset);

        _tokens.Setup(x => x.FindByHashAsync(
            VerificationToken.ComputeHash(raw), TokenPurpose.PasswordReset, default))
            .ReturnsAsync(token);

        _userMgr.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userMgr.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok");
        _userMgr.Setup(x => x.ResetPasswordAsync(user, "tok", "weak"))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = "Password too weak" }));

        var result = await Sut().Handle(new ResetPasswordCommand(raw, "weak"), default);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Password too weak");
    }
}
