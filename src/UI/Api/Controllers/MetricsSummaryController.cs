using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes basic runtime metrics (uptime, request totals, memory, GC counts) for operations and monitoring.
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
    /// Returns process uptime, total requests served, current memory usage, and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryResponseBuilder.Build(timeProvider, requestMetricsSnapshot);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
