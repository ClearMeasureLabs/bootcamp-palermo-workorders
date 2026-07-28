using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes process-level runtime metrics (uptime, request counts, memory, GC) for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(
    TimeProvider timeProvider,
    IRequestMetricsStore requestMetricsStore) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total requests served, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryBuilder.Build(timeProvider, requestMetricsStore);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
