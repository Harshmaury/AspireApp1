using MediatR;
using Attendance.Application.AttendanceRecord.Commands;
using Attendance.Application.AttendanceRecord.Queries;
using Attendance.Application.AttendanceSummary.Queries;
using Attendance.Application.Condonation.Commands;
using Attendance.Application.Condonation.Queries;
namespace Attendance.API.Endpoints;
public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api").RequireAuthorization();

        // --- Attendance Records ---
        grp.MapPost("/attendance", async (MarkAttendanceCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/attendance/{id}", new { id });
        });

        grp.MapGet("/attendance/student/{studentId}/course/{courseId}", async (
            Guid studentId, Guid courseId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentAttendanceQuery(studentId, courseId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/attendance/course/{courseId}/date/{date}", async (
            Guid courseId, DateOnly date, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCourseAttendanceByDateQuery(courseId, date, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        // --- Attendance Summary ---
        grp.MapGet("/attendance/summary/student/{studentId}", async (
            Guid studentId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentSummaryQuery(studentId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/attendance/summary/student/{studentId}/course/{courseId}", async (
            Guid studentId, Guid courseId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentCourseSummaryQuery(studentId, courseId, tenantId));
            return result is null
                ? Results.NotFound(new { success = false, error = new { code = "SUMMARY_NOT_FOUND", message = "No attendance summary found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/attendance/summary/shortages", async (Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetShortageListQuery(tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        // --- Condonation ---
        grp.MapPost("/attendance/condonation", async (CreateCondonationRequestCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/attendance/condonation/{id}", new { id });
        });

        grp.MapGet("/attendance/condonation/pending", async (Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPendingCondonationsQuery(tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/attendance/condonation/student/{studentId}", async (
            Guid studentId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetStudentCondonationsQuery(studentId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapPut("/attendance/condonation/{id}/approve", async (
            Guid id, ApproveCondonationCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { RequestId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapPut("/attendance/condonation/{id}/reject", async (
            Guid id, RejectCondonationCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { RequestId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });
    }
}
