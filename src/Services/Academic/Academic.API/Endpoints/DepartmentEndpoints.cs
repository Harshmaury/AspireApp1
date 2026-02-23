using Academic.Application.Department.Commands;
using Academic.Application.Department.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Academic.API.Middleware;
namespace Academic.API.Endpoints;
public static class DepartmentEndpoints
{
    public static void MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/departments");

        grp.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetAllDepartmentsQuery(tenantId), ct);
            return Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetDepartmentByIdQuery(id, tenantId), ct);
            return result is null
                ? Results.NotFound(new { success = false, data = (object?)null, error = new { code = "DEPARTMENT_NOT_FOUND", message = $"Department {id} not found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPost("/", async ([FromBody] CreateDepartmentCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { TenantId = tenantId };
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/departments/{id}", new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateDepartmentCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { Id = id, TenantId = tenantId };
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });
    }
}