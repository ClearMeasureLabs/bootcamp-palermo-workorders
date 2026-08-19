using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes process runtime metrics (uptime, request totals, memory, GC counts) for operations and monitoring.
/// Request totals include all inbound Kestrel HTTP requests recorded by server middleware.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(
    TimeProvider timeProvider,
    IRequestMetricsSnapshot requestMetricsSnapshot) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total HTTP requests served, memory usage, and GC collection counts for the current process.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = requestMetricsSnapshot.BuildSummary(timeProvider);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
