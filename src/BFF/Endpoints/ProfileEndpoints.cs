using System.Text.Json;

namespace BFF.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/mobile/profile", async (
            HttpContext httpContext,
            IHttpClientFactory factory) =>
        {
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "";
            var userId   = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()   ?? "";

            var student  = factory.CreateClient("student");
            var academic = factory.CreateClient("academic");

            ForwardHeaders(student,  tenantId, userId);
            ForwardHeaders(academic, tenantId, userId);

            var studentTask  = SafeGetAsync(student,  $"/api/students/{userId}");
            var academicTask = SafeGetAsync(academic, $"/api/academic/{userId}/profile");

            await Task.WhenAll(studentTask, academicTask);

            return Results.Ok(new
            {
                student  = studentTask.Result,
                academic = academicTask.Result
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
