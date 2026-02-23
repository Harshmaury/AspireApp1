using System.Text.Json;

namespace BFF.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/mobile/dashboard", async (
            HttpContext ctx,
            IHttpClientFactory factory) =>
        {
            var tenantId = ctx.Request.Headers["X-Tenant-Id"].ToString();
            var userId   = ctx.Request.Headers["X-User-Id"].ToString();

            var studentClient    = factory.CreateClient("student-api");
            var attendanceClient = factory.CreateClient("attendance-api");
            var feeClient        = factory.CreateClient("fee-api");

            AddForwardedHeaders(studentClient,    tenantId, userId);
            AddForwardedHeaders(attendanceClient, tenantId, userId);
            AddForwardedHeaders(feeClient,        tenantId, userId);

            var studentTask    = studentClient.GetAsync($"/api/students/{userId}/summary");
            var attendanceTask = attendanceClient.GetAsync($"/api/attendance/{userId}/summary");
            var feeTask        = feeClient.GetAsync($"/api/fees/{userId}/status");

            await Task.WhenAll(studentTask, attendanceTask, feeTask);

            var result = new
            {
                student    = await ParseOrNull(studentTask.Result),
                attendance = await ParseOrNull(attendanceTask.Result),
                fees       = await ParseOrNull(feeTask.Result)
            };

            return Results.Ok(result);
        }).RequireAuthorization();
    }

    private static void AddForwardedHeaders(HttpClient client, string tenantId, string userId)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Remove("X-User-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.Add("X-User-Id",   userId);
    }

    private static async Task<object?> ParseOrNull(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<object>(json);
    }
}
