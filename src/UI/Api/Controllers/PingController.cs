using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal JSON liveness probe for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/ping")]
[Route($"{ApiRoutes.VersionedApiPrefix}/ping")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class PingController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns a JSON payload with <c>pong</c> and the current UTC timestamp for reachability checks.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = new PingResponse(
            Pong: "pong",
            Timestamp: timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/ping</c> and <c>GET /api/v1.0/ping</c>.
/// </summary>
public record PingResponse(string Pong, string Timestamp);
