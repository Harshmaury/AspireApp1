using Microsoft.AspNetCore.Mvc;

namespace Attendance.API.Controllers;

[ApiController]
[Route("health")]
public sealed class RegionHealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public RegionHealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("region")]
    public IActionResult GetRegionHealth()
    {
        var regionId     = _configuration["REGION_ID"]            ?? "unknown";
        var regionRole   = _configuration["REGION_ROLE"]          ?? "unknown";
        var writeAllowed = _configuration["REGION_WRITE_ALLOWED"];

        return Ok(new
        {
            regionId,
            regionRole,
            writeAllowed = bool.TryParse(writeAllowed, out var b) && b,
            service      = "attendance-api",
            status       = "healthy"
        });
    }
}