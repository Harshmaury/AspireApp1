// UMS — University Management System
// Key:     UMS-IDENTITY-P2-007
// Service: Identity
// Layer:   Application / Features / Tenants / Commands
namespace Identity.Application.Features.Tenants.Commands;

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

// ── Suspend ───────────────────────────────────────────────────────────────

public sealed record SuspendTenantCommand(Guid TenantId, Guid ActorId)
    : IRequest<TenantLifecycleResult>;

// ── Reinstate ─────────────────────────────────────────────────────────────

public sealed record ReinstateTenantCommand(Guid TenantId, Guid ActorId)
    : IRequest<TenantLifecycleResult>;

// ── Upgrade ───────────────────────────────────────────────────────────────

public sealed record UpgradeTenantCommand(Guid TenantId, Guid ActorId, string NewTier)
    : IRequest<TenantLifecycleResult>;

// ── Shared result ─────────────────────────────────────────────────────────

public sealed record TenantLifecycleResult(
    bool   Succeeded,
    string Status,
    string? Error);

// ── Handlers ──────────────────────────────────────────────────────────────

internal sealed class SuspendTenantCommandHandler
    : IRequestHandler<SuspendTenantCommand, TenantLifecycleResult>
{
    private readonly ITenantRepository _tenants;
    private readonly IAuditLogger      _audit;

    public SuspendTenantCommandHandler(ITenantRepository tenants, IAuditLogger audit)
    { _tenants = tenants; _audit = audit; }

    public async Task<TenantLifecycleResult> Handle(
        SuspendTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenants.FindByIdAsync(request.TenantId, ct);
        if (tenant is null) return Fail("Tenant not found.");

        tenant.Suspend();
        await _tenants.UpdateAsync(tenant, ct);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.TenantSuspended,
            tenantId:  tenant.Id,
            userId:    request.ActorId,
            succeeded: true,
            details:   $"Tenant {tenant.Slug} suspended"), ct);

        return new TenantLifecycleResult(true, tenant.SubscriptionStatus.ToString(), null);
    }

    private static TenantLifecycleResult Fail(string error) =>
        new(false, string.Empty, error);
}

internal sealed class ReinstateTenantCommandHandler
    : IRequestHandler<ReinstateTenantCommand, TenantLifecycleResult>
{
    private readonly ITenantRepository _tenants;
    private readonly IAuditLogger      _audit;

    public ReinstateTenantCommandHandler(ITenantRepository tenants, IAuditLogger audit)
    { _tenants = tenants; _audit = audit; }

    public async Task<TenantLifecycleResult> Handle(
        ReinstateTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenants.FindByIdAsync(request.TenantId, ct);
        if (tenant is null) return Fail("Tenant not found.");

        tenant.Reinstate();
        await _tenants.UpdateAsync(tenant, ct);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.TenantReinstated,
            tenantId:  tenant.Id,
            userId:    request.ActorId,
            succeeded: true,
            details:   $"Tenant {tenant.Slug} reinstated"), ct);

        return new TenantLifecycleResult(true, tenant.SubscriptionStatus.ToString(), null);
    }

    private static TenantLifecycleResult Fail(string error) =>
        new(false, string.Empty, error);
}

internal sealed class UpgradeTenantCommandHandler
    : IRequestHandler<UpgradeTenantCommand, TenantLifecycleResult>
{
    private readonly ITenantRepository _tenants;
    private readonly IAuditLogger      _audit;

    public UpgradeTenantCommandHandler(ITenantRepository tenants, IAuditLogger audit)
    { _tenants = tenants; _audit = audit; }

    public async Task<TenantLifecycleResult> Handle(
        UpgradeTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenants.FindByIdAsync(request.TenantId, ct);
        if (tenant is null) return Fail("Tenant not found.");

        if (!Enum.TryParse<TenantTier>(request.NewTier, true, out var tier))
            return Fail($"Invalid tier: {request.NewTier}. Valid: Shared, Dedicated, Enterprise");

        tenant.Upgrade(tier);
        await _tenants.UpdateAsync(tenant, ct);

        await _audit.LogAsync(AuditLog.Create(
            action:    AuditActions.TenantUpgraded,
            tenantId:  tenant.Id,
            userId:    request.ActorId,
            succeeded: true,
            details:   $"Tenant {tenant.Slug} upgraded to {tier}"), ct);

        return new TenantLifecycleResult(true, tenant.Tier.ToString(), null);
    }

    private static TenantLifecycleResult Fail(string error) =>
        new(false, string.Empty, error);
}
