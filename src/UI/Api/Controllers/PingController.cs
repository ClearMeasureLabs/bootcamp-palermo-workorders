using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal plain-text probe for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/ping")]
[Route($"{ApiRoutes.VersionedApiPrefix}/ping")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class PingController : ControllerBase
{
    /// <summary>
    /// Returns the literal response body <c>pong</c> for reachability checks.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        new ContentResult
        {
            Content = "pong",
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
}
