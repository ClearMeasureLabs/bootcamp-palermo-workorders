using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for client debugging and operator diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns a JSON object reflecting method, path, query, host, remote IP, and selected request headers.
    /// Sensitive headers (for example <c>Authorization</c>, <c>Cookie</c>, API keys) are omitted.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = EchoRequestReflection.Build(HttpContext.Request, HttpContext.Connection);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
