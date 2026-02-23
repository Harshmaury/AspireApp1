using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class HealthResponseWriter
{
    public static async Task WriteDetailedJson(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        var result = new
        {
            status  = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks  = report.Entries.Select(e => new
            {
                name     = e.Key,
                status   = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                error    = e.Value.Exception?.Message
            })
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { WriteIndented = false }));
    }
}
