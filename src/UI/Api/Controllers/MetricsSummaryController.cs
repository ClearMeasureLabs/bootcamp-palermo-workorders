using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime metrics (uptime, request count, memory, GC collections) for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(TimeProvider timeProvider, IRequestMetricsCounter requestMetricsCounter)
    : ControllerBase
{
    /// <summary>
    /// Returns process uptime, total HTTP requests served, current managed-heap memory, and GC collection counts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = MetricsSummaryResponseBuilder.Build(
            timeProvider,
            requestMetricsCounter.TotalRequestsServed);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
