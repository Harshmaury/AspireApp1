using System.Text.Json;

namespace BFF.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/mobile/dashboard", async (
            HttpContext httpContext,
            IHttpClientFactory factory) =>
        {
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "";
            var userId   = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()   ?? "";

            var student    = factory.CreateClient("student");
            var fee        = factory.CreateClient("fee");
            var attendance = factory.CreateClient("attendance");

            ForwardHeaders(student,    tenantId, userId);
            ForwardHeaders(fee,        tenantId, userId);
            ForwardHeaders(attendance, tenantId, userId);

            var studentTask    = SafeGetAsync(student,    $"/api/students/{userId}");
            var feeTask        = SafeGetAsync(fee,        $"/api/fees/{userId}/summary");
            var attendanceTask = SafeGetAsync(attendance, $"/api/attendance/{userId}/summary");

            await Task.WhenAll(studentTask, feeTask, attendanceTask);

            return Results.Ok(new
            {
                student    = studentTask.Result,
                fee        = feeTask.Result,
                attendance = attendanceTask.Result
            });
        });
    }

    private static void ForwardHeaders(HttpClient client, string tenantId, string userId)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Remove("X-User-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.Add("X-User-Id",   userId);
    }

    private static async Task<object> SafeGetAsync(HttpClient client, string path)
    {
        try
        {
            var response = await client.GetAsync(path);
            if (!response.IsSuccessStatusCode)
                return new { error = $"Service returned {(int)response.StatusCode}", data = (object?)null };

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message, data = (object?)null };
        }
    }
}
