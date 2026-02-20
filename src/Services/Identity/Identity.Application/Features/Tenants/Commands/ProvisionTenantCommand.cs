using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Tenants.Commands;

public sealed record ProvisionTenantCommand(
    string Name,
    string Slug,
    string Tier,
    string Region) : IRequest<ProvisionTenantResult>;

public sealed record ProvisionTenantResult(Guid TenantId, string Slug, string Status);

public sealed class ProvisionTenantCommandHandler
    : IRequestHandler<ProvisionTenantCommand, ProvisionTenantResult>
{
    private readonly ITenantRepository _tenants;

    public ProvisionTenantCommandHandler(ITenantRepository tenants)
        => _tenants = tenants;

    public async Task<ProvisionTenantResult> Handle(
        ProvisionTenantCommand request, CancellationToken ct)
    {
        var existing = await _tenants.FindBySlugAsync(request.Slug, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Tenant '{request.Slug}' already exists.");

        var tier = Enum.TryParse<TenantTier>(request.Tier, true, out var t)
            ? t : TenantTier.Shared;

        var tenant = Tenant.Create(request.Name, request.Slug, tier, request.Region);
        await _tenants.AddAsync(tenant, ct);

        return new ProvisionTenantResult(tenant.Id, tenant.Slug, tenant.SubscriptionStatus.ToString());
    }
}
