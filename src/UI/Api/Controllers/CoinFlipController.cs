using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a random coin-flip probe for automated testing and operator diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/coinflip")]
[Route($"{ApiRoutes.VersionedApiPrefix}/coinflip")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class CoinFlipController : ControllerBase
{
    /// <summary>
    /// Returns either <c>heads</c> or <c>tails</c> as a plain-text response body.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        new ContentResult
        {
            Content = Random.Shared.Next(2) == 0 ? "heads" : "tails",
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
}
