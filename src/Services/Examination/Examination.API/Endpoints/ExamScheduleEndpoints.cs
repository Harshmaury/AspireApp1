using MediatR;
using Examination.Application.ExamSchedule.Commands;
namespace Examination.API.Endpoints;
public static class ExamScheduleEndpoints
{
    public static void MapExamScheduleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exam-schedules").RequireAuthorization();
        group.MapPost("/", async (CreateExamScheduleCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/exam-schedules/{id}", new { id });
        });
    }
}
