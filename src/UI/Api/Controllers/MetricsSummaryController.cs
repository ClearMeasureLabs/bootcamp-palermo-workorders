using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes aggregate runtime metrics (uptime, request totals, memory, GC counts) for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider,
    IRequestMetrics requestMetrics) : ControllerBase
{
    /// <summary>
    /// Returns environment name, uptime, total requests served, working-set memory (MB), and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryBuilder.Build(
            hostEnvironment.EnvironmentName,
            timeProvider,
            requestMetrics);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
