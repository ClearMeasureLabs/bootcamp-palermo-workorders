using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
<<<<<<< HEAD
=======
/// Response payload for the hello diagnostic endpoint.
/// </summary>
/// <param name="Message">Greeting text returned to callers.</param>
public record HelloResponse(string Message);

/// <summary>
>>>>>>> ec02aa23e3a0d12b1cca7c707c277167edab6c05
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
<<<<<<< HEAD

/// <summary>
/// JSON payload for <c>GET /api/hello</c> and <c>GET /api/v1.0/hello</c>.
/// </summary>
public record HelloResponse(string Message);
=======
>>>>>>> ec02aa23e3a0d12b1cca7c707c277167edab6c05
