// src/Services/Identity/Identity.Application/Features/Auth/Commands/VerifyEmailCommandHandler.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class VerifyEmailCommandHandler
    : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IVerificationTokenRepository _tokens;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogger                 _audit;
    private readonly IHttpContextAccessor         _http;

    public VerifyEmailCommandHandler(
        IVerificationTokenRepository tokens,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit,
        IHttpContextAccessor http)
    {
        _tokens      = tokens;
        _userManager = userManager;
        _audit       = audit;
        _http        = http;
    }

    public async Task<VerifyEmailResult> Handle(
        VerifyEmailCommand request, CancellationToken ct)
    {
        var ip   = GetIpAddress();
        var hash = VerificationToken.ComputeHash(request.RawToken);

        // 1. Find valid token by hash
        var token = await _tokens.FindByHashAsync(hash, TokenPurpose.EmailVerification, ct);

        if (token is null)
            throw new InvalidVerificationTokenException();

        if (token.IsExpired)
            throw new ExpiredVerificationTokenException();

        // 2. Load user
        var user = await _userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
            throw new InvalidVerificationTokenException();

        // 3. Confirm email via Identity
        user.EmailConfirmed = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return new VerifyEmailResult(false,
                string.Join("; ", result.Errors.Select(e => e.Description)));

        // 4. Mark token used
        token.MarkUsed();
        await _tokens.CreateAsync(token, ct);

        // 5. Audit
        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.EmailVerified,
            tenantId:  token.TenantId,
            userId:    user.Id,
            succeeded: true,
            ip:        ip,
            details:   $"Email verified: {user.Email}"), ct);

        return new VerifyEmailResult(true);
    }

    private string GetIpAddress()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return "unknown";
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
