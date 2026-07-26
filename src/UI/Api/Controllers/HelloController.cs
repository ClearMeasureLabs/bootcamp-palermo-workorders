using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a greeting endpoint for health checks and testing.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HelloController : ControllerBase
{
    /// <summary>
    /// Returns a JSON greeting message with HTTP 200.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        Ok(new HelloResponse("Hello, World!"));
}

/// <summary>
/// Response object for the hello endpoint.
/// </summary>
public record HelloResponse(string message);
