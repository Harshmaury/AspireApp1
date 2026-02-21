using MediatR;
using Faculty.Application.Faculty.Commands;
using Faculty.Application.Faculty.Queries;
using Faculty.Application.CourseAssignment.Commands;
using Faculty.Application.CourseAssignment.Queries;
using Faculty.Application.Publication.Commands;
using Faculty.Application.Publication.Queries;
namespace Faculty.API.Endpoints;
public static class FacultyEndpoints
{
    public static void MapFacultyEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api").RequireAuthorization();

        // --- Faculty ---
        grp.MapPost("/faculty", async (CreateFacultyCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/faculty/{id}", new { id });
        });

        grp.MapGet("/faculty", async (Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAllFacultyQuery(tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/{id}", async (Guid id, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyByIdQuery(id, tenantId));
            return result is null
                ? Results.NotFound(new { success = false, error = new { code = "FACULTY_NOT_FOUND", message = $"Faculty {id} not found." }, timestamp = DateTime.UtcNow })
                : Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/department/{departmentId}", async (Guid departmentId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyByDepartmentQuery(departmentId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapPatch("/faculty/{id}/designation", async (Guid id, UpdateFacultyDesignationCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { FacultyId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapPatch("/faculty/{id}/status", async (Guid id, UpdateFacultyStatusCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { FacultyId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapPatch("/faculty/{id}/experience", async (Guid id, UpdateFacultyExperienceCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { FacultyId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/nirf", async (Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyNirfQuery(tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        // --- Course Assignments ---
        grp.MapPost("/faculty/assignments", async (AssignCourseCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/faculty/assignments/{id}", new { id });
        });

        grp.MapDelete("/faculty/assignments/{id}", async (Guid id, Guid tenantId, IMediator mediator) =>
        {
            await mediator.Send(new UnassignCourseCommand(tenantId, id));
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/{facultyId}/assignments", async (Guid facultyId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyCourseAssignmentsQuery(facultyId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/{facultyId}/assignments/{academicYear}", async (Guid facultyId, string academicYear, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyCoursesByYearQuery(facultyId, academicYear, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });

        // --- Publications ---
        grp.MapPost("/faculty/publications", async (AddPublicationCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/faculty/publications/{id}", new { id });
        });

        grp.MapPatch("/faculty/publications/{id}/citations", async (Guid id, UpdateCitationCountCommand cmd, IMediator mediator) =>
        {
            await mediator.Send(cmd with { PublicationId = id });
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });

        grp.MapGet("/faculty/{facultyId}/publications", async (Guid facultyId, Guid tenantId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetFacultyPublicationsQuery(facultyId, tenantId));
            return Results.Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
        });
    }
}
