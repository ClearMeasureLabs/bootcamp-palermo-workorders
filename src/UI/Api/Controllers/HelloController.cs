using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Response payload for the hello diagnostic endpoint.
/// </summary>
/// <param name="Message">Greeting text returned to callers.</param>
public record HelloResponse(string Message);

/// <summary>
/// Exposes a minimal JSON greeting for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HelloController : ControllerBase
{
    /// <summary>
    /// Returns a JSON greeting for reachability checks.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new HelloResponse("Hello, World!"));
}
