using System.Runtime.InteropServices;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a redacted snapshot of runtime and host environment metadata for operators and automation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController(IOptions<EnvironmentStatusOptions> options) : ControllerBase
{
    /// <summary>
    /// Returns OS description, processor count, CLR/runtime identifiers, and allowlisted environment variable presence (values omitted).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var names = options.Value.IncludedEnvironmentVariables;
        var entries = new List<EnvironmentVariableStatusEntry>(names.Count);
        foreach (var name in names)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            var isSet = !string.IsNullOrEmpty(raw);
            entries.Add(new EnvironmentVariableStatusEntry(name, isSet));
        }

        var payload = new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            EnvironmentVariables: entries);

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
