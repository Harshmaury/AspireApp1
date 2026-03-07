// src/Services/Identity/Identity.Application/Features/Auth/Commands/ResendVerificationCommandHandler.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Identity.Application.Features.Auth.Commands;

internal sealed class ResendVerificationCommandHandler
    : IRequestHandler<ResendVerificationCommand, ResendVerificationResult>
{
    private readonly IVerificationTokenRepository _tokens;
    private readonly ITenantRepository            _tenants;
    private readonly IUserRepository              _users;
    private readonly IAuditLogger                 _audit;
    private readonly IHttpContextAccessor         _http;

    public ResendVerificationCommandHandler(
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

    public async Task<ResendVerificationResult> Handle(
        ResendVerificationCommand request, CancellationToken ct)
    {
        var ip = GetIpAddress();

        // 1. Resolve tenant
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct)
            ?? throw new TenantNotFoundException(Guid.Empty);

        // 2. Find user - return success even if not found (prevents email enumeration)
        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);

        if (user is null || user.EmailConfirmed)
            return new ResendVerificationResult(true);

        // 3. Invalidate all existing tokens for this user+purpose
        await _tokens.InvalidateAllForUserAsync(
            user.Id, TokenPurpose.EmailVerification, ct);

        // 4. Create new token
        var (token, rawToken) = VerificationToken.Create(
            userId:    user.Id,
            tenantId:  tenant.Id,
            purpose:   TokenPurpose.EmailVerification,
            ipAddress: ip);

        await _tokens.CreateAsync(token, ct);

        // 5. Raise event - Notification service sends the email via Kafka
        var domainEvent = new EmailVerificationRequestedEvent(
            userId:    user.Id,
            tenantId:  tenant.Id,
            email:     user.Email!,
            rawToken:  rawToken,
            expiresAt: token.ExpiresAt);

        // 6. Audit
        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.EmailVerified,
            tenantId:  tenant.Id,
            userId:    user.Id,
            succeeded: true,
            ip:        ip,
            details:   "Verification email resent"), ct);

        return new ResendVerificationResult(true);
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
