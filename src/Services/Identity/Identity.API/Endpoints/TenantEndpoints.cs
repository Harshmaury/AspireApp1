using Identity.Application.Features.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.API.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tenants", async (
            ProvisionTenantRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ProvisionTenantCommand(req.Name, req.Slug, req.Tier, req.Region), ct);
            return Results.Created($"/api/tenants/{result.TenantId}", result);
        })
        ;

        return app;
    }
}

public sealed record ProvisionTenantRequest(
    string Name,
    string Slug,
    string Tier = "Shared",
    string Region = "default");
