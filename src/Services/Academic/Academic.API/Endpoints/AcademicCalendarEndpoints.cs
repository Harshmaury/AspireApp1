using Academic.Application.AcademicCalendar.Commands;
using Academic.Application.AcademicCalendar.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Academic.API.Middleware;
namespace Academic.API.Endpoints;
public static class AcademicCalendarEndpoints
{
    public static void MapAcademicCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/academic-calendars");

        grp.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetAllAcademicCalendarsQuery(tenantId), ct);
            return Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/active", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetActiveCalendarQuery(tenantId), ct);
            return result is null
                ? Results.NotFound(new { success = false, data = (object?)null, error = new { code = "NO_ACTIVE_CALENDAR", message = "No active academic calendar found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetAcademicCalendarByIdQuery(id, tenantId), ct);
            return result is null
                ? Results.NotFound(new { success = false, data = (object?)null, error = new { code = "CALENDAR_NOT_FOUND", message = $"Calendar {id} not found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPost("/", async ([FromBody] CreateAcademicCalendarCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { TenantId = tenantId };
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/academic-calendars/{id}", new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPatch("/{id:guid}/activate", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            await mediator.Send(new ActivateAcademicCalendarCommand(id, tenantId), ct);
            return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });
    }
}