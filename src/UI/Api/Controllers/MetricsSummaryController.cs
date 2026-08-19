using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes basic runtime metrics (uptime, request count, memory, GC collections) for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(TimeProvider timeProvider, IRequestMetricsCollector collector) : ControllerBase
{
    /// <summary>
    /// Returns a JSON snapshot of process uptime, total requests served, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryResponseBuilder.Build(timeProvider, collector.TotalRequestsServed);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
