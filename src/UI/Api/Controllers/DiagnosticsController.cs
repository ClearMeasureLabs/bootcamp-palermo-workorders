using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime diagnostics (environment, uptime, feature flags) for operations and support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/diagnostics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/diagnostics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class DiagnosticsController(
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider,
    IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : ControllerBase
{
    /// <summary>
    /// Returns environment name, process uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>), and configured feature flags.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        var payload = new DiagnosticsResponse(
            Environment: hostEnvironment.EnvironmentName,
            Uptime: healthSlice.Uptime,
            FeatureFlags: featureFlagsOptions.Value);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/diagnostics</c> and <c>GET /api/v1.0/diagnostics</c>.
/// </summary>
public sealed record DiagnosticsResponse(
    string Environment,
    TimeSpan Uptime,
    DiagnosticsFeatureFlagsOptions FeatureFlags);
