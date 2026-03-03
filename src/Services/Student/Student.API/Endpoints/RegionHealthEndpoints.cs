namespace Student.API.Endpoints;

public static class RegionHealthEndpoints
{
    public static void MapRegionHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health/region", (IConfiguration config) =>
        {
            var regionId     = config["REGION_ID"]            ?? "unknown";
            var regionRole   = config["REGION_ROLE"]          ?? "unknown";
            var writeAllowed = config["REGION_WRITE_ALLOWED"];

            return Results.Ok(new
            {
                regionId,
                regionRole,
                writeAllowed = bool.TryParse(writeAllowed, out var b) && b,
                service      = "student-api",
                status       = "healthy"
            });
        });
    }
}