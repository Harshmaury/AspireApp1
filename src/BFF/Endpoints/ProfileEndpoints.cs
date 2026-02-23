using System.Text.Json;

namespace BFF.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/mobile/profile", async (
            HttpContext ctx,
            IHttpClientFactory factory) =>
        {
            var tenantId = ctx.Request.Headers["X-Tenant-Id"].ToString();
            var userId   = ctx.Request.Headers["X-User-Id"].ToString();

            var studentClient  = factory.CreateClient("student-api");
            var academicClient = factory.CreateClient("academic-api");

            AddForwardedHeaders(studentClient,  tenantId, userId);
            AddForwardedHeaders(academicClient, tenantId, userId);

            var studentTask  = studentClient.GetAsync($"/api/students/{userId}/profile");
            var academicTask = academicClient.GetAsync($"/api/academic/{userId}/profile");

            await Task.WhenAll(studentTask, academicTask);

            var result = new
            {
                student  = await ParseOrNull(studentTask.Result),
                academic = await ParseOrNull(academicTask.Result)
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
