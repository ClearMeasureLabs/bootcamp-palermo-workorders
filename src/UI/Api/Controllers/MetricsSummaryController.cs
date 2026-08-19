using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON runtime metrics snapshot for operators and monitoring integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(TimeProvider timeProvider, IRequestMetricsCollector collector) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total requests served, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryResponseBuilder.Build(timeProvider, collector.TotalRequestsServed);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
