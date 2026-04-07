using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes basic runtime metrics for operators. <see cref="TotalRequestsServed"/> counts every HTTP request
/// that reaches the host pipeline after request-counting middleware (static files, Blazor, APIs, health, etc.), per process.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(TimeProvider timeProvider, IRequestMetrics requestMetrics) : ControllerBase
{
    /// <summary>
    /// Returns uptime, memory, GC stats, and total HTTP requests served for this process.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var process = Process.GetCurrentProcess();
        var processStartUtc = new DateTimeOffset(process.StartTime).ToUniversalTime();
        var now = timeProvider.GetUtcNow();
        var uptime = now - processStartUtc;

        var gcInfo = GC.GetGCMemoryInfo(GCKind.Any);
        var payload = new MetricsSummaryResponse
        {
            Uptime = uptime,
            MemoryBytes = process.WorkingSet64,
            HeapSizeBytes = gcInfo.HeapSizeBytes,
            GcCollections = new GcCollectionCounts
            {
                Gen0 = GC.CollectionCount(0),
                Gen1 = GC.CollectionCount(1),
                Gen2 = GC.CollectionCount(2)
            },
            TotalRequestsServed = requestMetrics.TotalCount,
            TimestampUtc = now.UtcDateTime
        };

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
