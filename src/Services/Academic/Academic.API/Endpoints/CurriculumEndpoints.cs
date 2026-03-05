using Academic.API.Middleware;
using Academic.Application.Curriculum.Commands;
using Academic.Application.Curriculum.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Academic.API.Endpoints;

public static class CurriculumEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCurriculumEndpoints()
        {
            var grp = app.MapGroup("/api/curricula");

            grp.MapGet("/programme/{programmeId:guid}", async (Guid programmeId, string version, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
            {
                var tenantId = ctx.GetTenantId();
                var result = await mediator.Send(new GetCurriculumByProgrammeQuery(programmeId, tenantId, version), ct);
                return Results.Ok(new { success = true, data = result, error = (object?)null, timestamp = DateTime.UtcNow });
            });

            grp.MapPost("/", async ([FromBody] AddCourseToCurriculumCommand cmd, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
            {
                var tenantId = ctx.GetTenantId();
                var command = cmd with { TenantId = tenantId };
                var id = await mediator.Send(command, ct);
                return Results.Created($"/api/curricula/{id}", new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
            });

            grp.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
            {
                var tenantId = ctx.GetTenantId();
                await mediator.Send(new RemoveCurriculumEntryCommand(id, tenantId), ct);
                return Results.Ok(new { success = true, data = new { id }, error = (object?)null, timestamp = DateTime.UtcNow });
            });
        }
    }
}