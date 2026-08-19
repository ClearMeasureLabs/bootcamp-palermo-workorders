using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes lightweight runtime metrics (uptime, request totals, memory, GC) for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsController(
    TimeProvider timeProvider,
    IRuntimeMetricsCollector metricsCollector) : ControllerBase
{
    /// <summary>
    /// Returns process uptime, total HTTP requests served, current memory usage, and GC collection counts.
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var payload = metricsCollector.BuildSummary(timeProvider);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
