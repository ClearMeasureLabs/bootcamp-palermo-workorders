using System.Diagnostics;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes basic runtime metrics (uptime, request totals, memory, GC) for operators and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class MetricsSummaryController(TimeProvider timeProvider, IHttpRequestMetrics httpRequestMetrics) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total HTTP requests counted by the host, working set, allocated bytes, and GC collection counts.
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var payload = new MetricsSummaryResponse(
            Uptime: healthSlice.Uptime,
            TotalRequestsServed: httpRequestMetrics.TotalRequestsServed,
            WorkingSetBytes: process.WorkingSet64,
            TotalAllocatedBytes: GC.GetTotalAllocatedBytes(false),
            GcGen0Collections: GC.CollectionCount(0),
            GcGen1Collections: GC.CollectionCount(1),
            GcGen2Collections: GC.CollectionCount(2));
        return ConditionalGetEtag.JsonContent(payload);
    }
}
