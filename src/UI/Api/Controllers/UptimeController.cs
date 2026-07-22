using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes process uptime information for monitoring and diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/uptime")]
[Route($"{ApiRoutes.VersionedApiPrefix}/uptime")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class UptimeController : ControllerBase
{
    /// <summary>
    /// Returns the process uptime in seconds.
    /// </summary>
    /// <returns>JSON object containing uptimeSeconds property.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var startTime = Process.GetCurrentProcess().StartTime;
        var uptime = DateTime.UtcNow - startTime.ToUniversalTime();
        var uptimeSeconds = (long)uptime.TotalSeconds;

        return Ok(new { uptimeSeconds });
    }
}
