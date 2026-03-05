namespace Academic.API.Endpoints;

public static class RegionHealthEndpoints
{
    extension(WebApplication app)
    {
        public void MapRegionHealthEndpoints()
        {
            app.MapGet("/health/region", (IConfiguration config) =>
            {
                var regionId = config["REGION_ID"] ?? "unknown";
                var regionRole = config["REGION_ROLE"] ?? "unknown";
                var writeAllowed = config["REGION_WRITE_ALLOWED"];

                return Results.Ok(new
                {
                    regionId,
                    regionRole,
                    writeAllowed = bool.TryParse(writeAllowed, out var b) && b,
                    service = "academic-api",
                    status = "healthy"
                });
            });
        }
    }
}