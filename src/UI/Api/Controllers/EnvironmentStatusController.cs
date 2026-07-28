using System.Runtime.InteropServices;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes OS, CPU, runtime, and allowlisted environment variable names (values redacted) for operations and support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController(IOptions<RuntimeEnvironmentStatusOptions> options) : ControllerBase
{
    /// <summary>
    /// Returns runtime environment diagnostics. Variable values are never included; only configured allowlist names with <see cref="RuntimeEnvironmentVariableEntry.ValueRedacted"/> set.
    /// Supports weak ETag and <c>If-None-Match</c> → 304 when the representation is unchanged (same inputs produce the same JSON).
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var names = ResolveAllowlistedNames(options.Value);
        var entries = names
            .Select(n => new RuntimeEnvironmentVariableEntry(Name: n, ValueRedacted: true))
            .ToList();

        var payload = new RuntimeEnvironmentResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            EnvironmentVariables: entries);

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return ConditionalGetEtag.JsonContent(payload);
    }

    internal static List<string> ResolveAllowlistedNames(RuntimeEnvironmentStatusOptions opts)
    {
        var source = opts.VariableNames is { Length: > 0 }
            ? opts.VariableNames
            : RuntimeEnvironmentStatusOptions.DefaultVariableNames;

        var result = new List<string>(Math.Min(source.Length, RuntimeEnvironmentStatusOptions.MaxVariableNames));
        foreach (var raw in source)
        {
            if (result.Count >= RuntimeEnvironmentStatusOptions.MaxVariableNames)
            {
                break;
            }

            var name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var duplicate = false;
            foreach (var existing in result)
            {
                if (string.Equals(existing, name, StringComparison.Ordinal))
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                result.Add(name);
            }
        }

        return result;
    }
}
