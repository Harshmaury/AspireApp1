// src/Services/Identity/Identity.Application/Features/Auth/Commands/ResetPasswordCommandHandler.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IVerificationTokenRepository _tokens;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogger                 _audit;
    private readonly IHttpContextAccessor         _http;

    public ResetPasswordCommandHandler(
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

    public async Task<ResetPasswordResult> Handle(
        ResetPasswordCommand request, CancellationToken ct)
    {
        var ip   = GetIpAddress();
        var hash = VerificationToken.ComputeHash(request.RawToken);

        // 1. Find valid token by hash
        var token = await _tokens.FindByHashAsync(
            hash, TokenPurpose.PasswordReset, ct);

        if (token is null)
            throw new InvalidVerificationTokenException();

        if (token.IsExpired)
            throw new ExpiredVerificationTokenException();

        // 2. Load user
        var user = await _userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
            throw new InvalidVerificationTokenException();

        // 3. Reset password via Identity (handles hashing)
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result     = await _userManager.ResetPasswordAsync(
            user, resetToken, request.NewPassword);

        if (!result.Succeeded)
            return new ResetPasswordResult(false,
                string.Join("; ", result.Errors.Select(e => e.Description)));

        // 4. Mark token used - prevents replay attacks
        token.MarkUsed();
        await _tokens.CreateAsync(token, ct);

        // 5. Invalidate all remaining reset tokens for this user
        await _tokens.InvalidateByUserAsync(
            user.Id, TokenPurpose.PasswordReset, ct);

        // 6. Raise event - Notification service sends confirmation email via Kafka
        var domainEvent = new PasswordResetCompletedEvent(
            userId:    user.Id,
            tenantId:  token.TenantId,
            email:     user.Email!,
            ipAddress: ip);

        // 7. Audit
        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.PasswordReset,
            tenantId:  token.TenantId,
            userId:    user.Id,
            succeeded: true,
            ip:        ip,
            details:   "Password reset completed"), ct);

        return new ResetPasswordResult(true);
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
