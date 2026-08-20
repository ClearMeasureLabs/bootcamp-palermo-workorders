using System.Runtime.InteropServices;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime environment diagnostics for operators and support tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class StatusController : ControllerBase
{
    /// <summary>
    /// Returns OS, processor, CLR, and redacted allowlisted environment variable names.
    /// </summary>
    [HttpGet("environment")]
    public IActionResult GetEnvironment()
    {
        var payload = new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            EnvironmentVariables: EnvironmentVariableSnapshotBuilder.Build());
        return ConditionalJson(payload);
    }

    private IActionResult ConditionalJson<T>(T payload)
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload!);
    }
}
