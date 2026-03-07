// src/Services/Identity/Identity.Application/Features/Auth/Commands/ValidateCredentialsCommand.cs
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Identity.Application.Features.Auth.Commands;

public sealed record ValidateCredentialsCommand(
    string TenantSlug,
    string Email,
    string Password
) : IRequest<ValidateCredentialsResult>;

public sealed record ValidateCredentialsResult(
    bool Succeeded,
    ApplicationUser? User,
    Tenant? Tenant,
    IList<string>? Roles,
    string? Error
);

internal sealed class ValidateCredentialsCommandHandler
    : IRequestHandler<ValidateCredentialsCommand, ValidateCredentialsResult>
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly IAuditLogger _audit;
    private readonly IHttpContextAccessor _http;
    private readonly bool _writeAllowed;

    public ValidateCredentialsCommandHandler(
        IUserRepository users,
        ITenantRepository tenants,
        IAuditLogger audit,
        IHttpContextAccessor http,
        IConfiguration configuration)
    {
        _users        = users;
        _tenants      = tenants;
        _audit        = audit;
        _http         = http;
        _writeAllowed = bool.TryParse(
            configuration["REGION_WRITE_ALLOWED"], out var b) && b;
    }

    public async Task<ValidateCredentialsResult> Handle(
        ValidateCredentialsCommand request, CancellationToken ct)
    {
        var ip     = GetIpAddress();
        var device = GetDeviceInfo();

        // 1. Tenant lookup
        var tenant = await _tenants.FindBySlugAsync(request.TenantSlug, ct);
        if (tenant is null)
        {
            await WriteAuditAsync(AuditActions.LoginFailed,
                tenantId: Guid.Empty,
                userId: null,
                ip: ip,
                succeeded: false,
                details: $"Tenant not found: {request.TenantSlug}", ct);
            return Fail("Invalid credentials.");
        }

        // 2. Subscription guard
        if (tenant.SubscriptionStatus == SubscriptionStatus.Suspended ||
            tenant.SubscriptionStatus == SubscriptionStatus.Cancelled)
        {
            await WriteAuditAsync(AuditActions.LoginFailed,
                tenantId: tenant.Id,
                userId: null,
                ip: ip,
                succeeded: false,
                details: $"Tenant account not available: {tenant.SubscriptionStatus}", ct);
            return Fail("Tenant account is not available.");
        }

        // 3. User lookup scoped to tenant
        var user = await _users.FindByEmailAsync(tenant.Id, request.Email, ct);
        if (user is null)
        {
            await WriteAuditAsync(AuditActions.LoginFailed,
                tenantId: tenant.Id,
                userId: null,
                ip: ip,
                succeeded: false,
                details: "User not found", ct);
            return Fail("Invalid credentials.");
        }

        // 4. User active guard
        if (!user.IsActive)
        {
            await WriteAuditAsync(AuditActions.LoginFailed,
                tenantId: tenant.Id,
                userId: user.Id,
                ip: ip,
                succeeded: false,
                details: "User account is inactive", ct);
            return Fail("Invalid credentials.");
        }

        // 5. Lockout-aware password check
        var passwordResult = await _users.CheckPasswordWithLockoutAsync(
            user, request.Password, ct);

        if (passwordResult == PasswordCheckResult.LockedOut)
        {
            await WriteAuditAsync(AuditActions.Lockout,
                tenantId: tenant.Id,
                userId: user.Id,
                ip: ip,
                succeeded: false,
                details: "Account locked out", ct);
            return Fail("Account is temporarily locked. Try again later.");
        }

        if (passwordResult == PasswordCheckResult.Failed)
        {
            await WriteAuditAsync(AuditActions.LoginFailed,
                tenantId: tenant.Id,
                userId: user.Id,
                ip: ip,
                succeeded: false,
                details: "Invalid password", ct);
            return Fail("Invalid credentials.");
        }

        // 6. Fetch roles for token claims
        var roles = await _users.GetRolesAsync(user, ct);

        // 7. Record login only on PRIMARY region
        if (_writeAllowed)
        {
            user.RecordLogin();
            await _users.UpdateAsync(user);
        }

        // 8. Write success audit
        await WriteAuditAsync(AuditActions.Login,
            tenantId: tenant.Id,
            userId: user.Id,
            ip: ip,
            succeeded: true,
            details: $"device={device}", ct);

        return new ValidateCredentialsResult(true, user, tenant, roles, null);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task WriteAuditAsync(
        string action,
        Guid tenantId,
        Guid? userId,
        string ip,
        bool succeeded,
        string details,
        CancellationToken ct)
    {
        var entry = AuditLog.Create(
            action:    action,
            tenantId:  tenantId,
            userId:    userId,
            succeeded: succeeded,
            ip:        ip,
            ua:        GetDeviceInfo(),
            details:   details);

        await _audit.LogAsync(entry, ct);
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

    private string GetDeviceInfo()
    {
        var ua = _http.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault()
                 ?? "unknown";
        return ua.Length > 200 ? ua[..200] : ua;
    }

    private static ValidateCredentialsResult Fail(string error) =>
        new(false, null, null, null, error);
}
