using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a plain-text inspirational quote for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/quote")]
[Route($"{ApiRoutes.VersionedApiPrefix}/quote")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class QuoteController : ControllerBase
{
    /// <summary>
    /// Returns a fixed inspirational quote as plain text.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        new ContentResult
        {
            Content = QuoteConstants.DefaultText,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
}
