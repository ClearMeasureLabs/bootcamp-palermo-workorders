using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Minimal connectivity probe returning plain text <c>pong</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/ping")]
[Route($"{ApiRoutes.VersionedApiPrefix}/ping")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class PingController : ControllerBase
{
    /// <summary>
    /// Returns HTTP 200 with body <c>pong</c> as plain text.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return new ContentResult
        {
            Content = "pong",
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
