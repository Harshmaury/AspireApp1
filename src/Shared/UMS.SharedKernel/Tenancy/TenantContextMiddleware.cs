// UMS — University Management System
// Key:     UMS-SHARED-P1-001
// Service: SharedKernel
// Layer:   Infrastructure / Tenancy
namespace UMS.SharedKernel.Tenancy;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves tenant identity from JWT claims and populates the scoped
/// <see cref="TenantContext"/> early in the pipeline so every downstream
/// component (DbContext query filters, domain services, handlers) sees a
/// consistent, immutable tenant for the lifetime of the request.
///
/// Expected JWT claims:
///   tenant_id  — GUID string
///   tenant_slug — short human-readable identifier  (e.g. "mit", "iit-bombay")
///   tenant_tier — plan tier                        (e.g. "standard", "premium")
/// </summary>
public sealed class TenantContextMiddleware
{
    private const string ClaimTenantId   = "tenant_id";
    private const string ClaimTenantSlug = "tenant_slug";
    private const string ClaimTenantTier = "tenant_tier";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(
        RequestDelegate next,
        ILogger<TenantContextMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Cast to the concrete mutable type — only the middleware may mutate it
        if (tenantContext is not TenantContext mutable)
        {
            await _next(context);
            return;
        }

        var user = context.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var idClaim   = user.FindFirst(ClaimTenantId)?.Value;
            var slugClaim = user.FindFirst(ClaimTenantSlug)?.Value ?? string.Empty;
            var tierClaim = user.FindFirst(ClaimTenantTier)?.Value ?? "standard";

            if (Guid.TryParse(idClaim, out var tenantId))
            {
                mutable.SetTenant(tenantId, slugClaim, tierClaim);
            }
            else
            {
                _logger.LogWarning(
                    "Authenticated request is missing a valid '{Claim}' claim. " +
                    "Tenant context will not be resolved. Path={Path}",
                    ClaimTenantId, context.Request.Path);
            }
        }

        await _next(context);
    }
}
