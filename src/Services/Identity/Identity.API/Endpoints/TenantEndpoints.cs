// UMS — University Management System
// Key:     UMS-IDENTITY-P2-010
// Service: Identity
// Layer:   API / Endpoints
namespace Identity.API.Endpoints;

using Identity.Application.Features.Tenants.Commands;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .RequireAuthorization(p => p.RequireRole("SuperAdmin"));

        // ── Provision ─────────────────────────────────────────────────────
        group.MapPost("/", ProvisionAsync)
            .WithName("ProvisionTenant");

        // ── Lookup ────────────────────────────────────────────────────────
        group.MapGet("/{slug}", GetBySlugAsync)
            .WithName("GetTenantBySlug");

        // ── Lifecycle ─────────────────────────────────────────────────────
        group.MapPut("/{tenantId:guid}/suspend", SuspendAsync)
            .WithName("SuspendTenant");

        group.MapPut("/{tenantId:guid}/reinstate", ReinstateAsync)
            .WithName("ReinstateTenant");

        group.MapPut("/{tenantId:guid}/upgrade", UpgradeAsync)
            .WithName("UpgradeTenant");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private static async Task<IResult> ProvisionAsync(
        [FromBody] ProvisionTenantRequest req,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ProvisionTenantCommand(req.Name, req.Slug, req.Tier, req.Region), ct);
        return Results.Created($"/api/tenants/{result.TenantId}", result);
    }

    private static async Task<IResult> GetBySlugAsync(
        string slug,
        ITenantRepository tenants,
        CancellationToken ct)
    {
        var tenant = await tenants.FindBySlugAsync(slug, ct);
        if (tenant is null) return Results.NotFound();

        return Results.Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Tier,
            tenant.SubscriptionStatus,
            tenant.IsActive,
            tenant.MaxUsers,
            tenant.Region,
            tenant.CreatedAt,
            Features = new
            {
                tenant.Features.AllowSelfRegistration,
                tenant.Features.AllowGuestAccess,
                tenant.Features.EnableMfa,
                tenant.Features.EnableAuditLog
            }
        });
    }

    private static async Task<IResult> SuspendAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorId = GetActorId(principal);
        var result  = await sender.Send(new SuspendTenantCommand(tenantId, actorId), ct);

        return result.Succeeded
            ? Results.Ok(new { result.Status })
            : Results.BadRequest(new { result.Error });
    }

    private static async Task<IResult> ReinstateAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorId = GetActorId(principal);
        var result  = await sender.Send(new ReinstateTenantCommand(tenantId, actorId), ct);

        return result.Succeeded
            ? Results.Ok(new { result.Status })
            : Results.BadRequest(new { result.Error });
    }

    private static async Task<IResult> UpgradeAsync(
        Guid tenantId,
        [FromBody] UpgradeTenantRequest req,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorId = GetActorId(principal);
        var result  = await sender.Send(
            new UpgradeTenantCommand(tenantId, actorId, req.Tier), ct);

        return result.Succeeded
            ? Results.Ok(new { result.Status })
            : Results.BadRequest(new { result.Error });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Guid GetActorId(ClaimsPrincipal p)
    {
        var sub = p.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? p.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    // ── Request records ───────────────────────────────────────────────────

    public sealed record ProvisionTenantRequest(
        string Name,
        string Slug,
        string Tier   = "Shared",
        string Region = "default");

    public sealed record UpgradeTenantRequest(string Tier);
}
