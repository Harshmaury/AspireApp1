// src/Services/Identity/Identity.Application/Features/Auth/Commands/ForgotPasswordCommandHandler.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IVerificationTokenRepository _tokens;
    private readonly ITenantRepository            _tenants;
    private readonly IUserRepository              _users;
    private readonly IAuditLogger                 _audit;
    private readonly IHttpContextAccessor         _http;

    public ForgotPasswordCommandHandler(
        IVerificationTokenRepository tokens,
        ITenantRepository tenants,
        IUserRepository users,
        IAuditLogger audit,
        IHttpContextAccessor http)
    {
        _tokens  = tokens;
        _tenants = tenants;
        _users   = users;
        _audit   = audit;
        _http    = http;
    }

    public async Task<ForgotPasswordResult> Handle(
        ForgotPasswordCommand request, CancellationToken ct)
    {
        var ip = GetIpAddress();

        // 1. Resolve tenant
        // Always return success to prevent tenant enumeration
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null)
            return new ForgotPasswordResult(true);

        // 2. Find user
        // Always return success to prevent email enumeration
        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null || !user.IsActive)
            return new ForgotPasswordResult(true);

        // 3. Invalidate all existing password reset tokens for this user
        await _tokens.InvalidateAllForUserAsync(
            user.Id, TokenPurpose.PasswordReset, ct);

        // 4. Create new reset token - expires in 1 hour
        var (token, rawToken) = VerificationToken.Create(
            userId:    user.Id,
            tenantId:  tenant.Id,
            purpose:   TokenPurpose.PasswordReset,
            ipAddress: ip);

        await _tokens.CreateAsync(token, ct);

        // 5. Raise event - Notification service sends reset email via Kafka
        var domainEvent = new PasswordResetRequestedEvent(
            userId:    user.Id,
            tenantId:  tenant.Id,
            email:     user.Email!,
            rawToken:  rawToken,
            expiresAt: token.ExpiresAt,
            ipAddress: ip);

        // 6. Audit
        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.PasswordReset,
            tenantId:  tenant.Id,
            userId:    user.Id,
            succeeded: true,
            ip:        ip,
            details:   "Password reset requested"), ct);

        return new ForgotPasswordResult(true);
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
