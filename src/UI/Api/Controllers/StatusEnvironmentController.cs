using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime environment metadata for operations and support diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class StatusEnvironmentController(IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns OS description, processor count, CLR version, and curated environment variable names with redacted values.
    /// Supports <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusResponseBuilder.Build(configuration);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
