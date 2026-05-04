using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns a JSON snapshot of the current HTTP request (method, path, query, selected headers, connection metadata) for debugging.
/// When <c>ApiKeyAuthentication</c> is enabled, callers must send the same <c>X-Api-Key</c> as for other protected <c>/api/*</c> routes (for example <c>/api/diagnostics</c>).
/// This controller applies <see cref="ApiRateLimiting.PolicyName"/> so requests count toward the same sliding-window limit as other decorated API controllers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class EchoController : ControllerBase
{
    /// <summary>
    /// Reflects non-sensitive request metadata as JSON.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = EchoResponseBuilder.Build(HttpContext);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
