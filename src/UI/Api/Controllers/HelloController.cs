using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal Hello World endpoint.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HelloController : ControllerBase
{
    /// <summary>
    /// Returns <c>{"message": "Hello, World!"}</c> with HTTP 200.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        new OkObjectResult(new { message = "Hello, World!" });
}
