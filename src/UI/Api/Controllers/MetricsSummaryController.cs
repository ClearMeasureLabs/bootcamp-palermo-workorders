using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Operational runtime snapshot (uptime, request totals, GC, managed memory).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class MetricsSummaryController(
    TimeProvider timeProvider,
    IRequestCounters requestCounters,
    IProcessRuntimeMetrics runtimeMetrics) : ControllerBase
{
    /// <summary>
    /// Returns process uptime (<see cref="SimpleHealthResponseBuilder"/>), middleware request total, managed memory, and GC collection counts at response time.
    /// </summary>
    [HttpGet("summary")]
    public IActionResult Get()
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider).Uptime;
        var payload = new MetricsSummaryResponse(
            uptime,
            requestCounters.TotalRequests,
            runtimeMetrics.ManagedMemoryBytes,
            runtimeMetrics.GcGen0Collections,
            runtimeMetrics.GcGen1Collections,
            runtimeMetrics.GcGen2Collections);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
