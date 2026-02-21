using Academic.Application.Course.Commands;
using Academic.Application.Course.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Academic.API.Middleware;
namespace Academic.API.Endpoints;
public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/courses").RequireAuthorization();

        grp.MapGet("/department/{departmentId:guid}", async (Guid departmentId, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetCoursesByDepartmentQuery(departmentId, tenantId), ct);
            return Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var result = await mediator.Send(new GetCourseByIdQuery(id, tenantId), ct);
            return result is null
                ? Results.NotFound(new { success = false, data = (object?)null, error = new { code = "COURSE_NOT_FOUND", message = $"Course {id} not found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPost("/", async ([FromBody] CreateCourseCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { TenantId = tenantId };
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/courses/{id}", new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateCourseCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            var command = cmd with { Id = id, TenantId = tenantId };
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });

        grp.MapPatch("/{id:guid}/publish", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var tenantId = ctx.GetTenantId();
            await mediator.Send(new PublishCourseCommand(id, tenantId), ct);
            return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
        });
    }
}