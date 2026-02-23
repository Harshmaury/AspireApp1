using Academic.Application.Programme.Commands;
using Academic.Application.Programme.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Academic.API.Middleware;
namespace Academic.API.Endpoints;
public static class ProgrammeEndpoints
{
    public static void MapProgrammeEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/programmes");

        grp.MapGet("/department/{departmentId:guid}", async (Guid departmentId, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetProgrammesByDepartmentQuery(departmentId, tenantId), ct);
            return Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetProgrammeByIdQuery(id, tenantId), ct);
            return result is null
                ? Results.NotFound(new { success = false, data = (object?)null, error = new { code = "PROGRAMME_NOT_FOUND", message = $"Programme {id} not found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPost("/", async ([FromBody] CreateProgrammeCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { TenantId = tenantId };
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/programmes/{id}", new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProgrammeCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { Id = id, TenantId = tenantId };
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });
    }
}