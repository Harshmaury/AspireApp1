using MediatR;
using Fee.Application.Scholarship.Commands;
namespace Fee.API.Endpoints;
public static class ScholarshipEndpoints
{
    public static void MapScholarshipEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/scholarships").RequireAuthorization();
        group.MapPost("/", async (CreateScholarshipCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/scholarships/{id}", new { id });
        });
    }
}
