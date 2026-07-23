using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal JSON probe for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class StatusController : ControllerBase
{
    /// <summary>
    /// Returns a JSON payload with <c>status</c> set to <c>ok</c> for reachability checks.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new StatusResponse { Status = "ok" });
}

/// <summary>
/// JSON payload for <c>GET /api/status</c>.
/// </summary>
public sealed class StatusResponse
{
    /// <summary>
    /// Application status indicator; always <c>ok</c> when the endpoint is reachable.
    /// </summary>
    public required string Status { get; init; }
}
