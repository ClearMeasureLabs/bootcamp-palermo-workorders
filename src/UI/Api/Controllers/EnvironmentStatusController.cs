using System.Runtime.InteropServices;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a redacted snapshot of runtime environment details for diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController : ControllerBase
{
    /// <summary>
    /// Returns OS, processor, CLR, and selected environment variable presence (values redacted).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusResponseBuilder.Build(Environment.GetEnvironmentVariable);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and versioned equivalent.
/// </summary>
public sealed record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

internal static class EnvironmentStatusResponseBuilder
{
    internal static readonly string[] DiagnosticEnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT",
        "DATABASE_ENGINE",
        "DOTNET_RUNNING_IN_CONTAINER",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model",
        "ASPNETCORE_URLS",
        "HOSTNAME",
        "COMPUTERNAME",
        "POD_NAME",
        "KUBERNETES_SERVICE_HOST"
    ];

    internal const string RedactedValue = "(redacted)";

    internal static EnvironmentStatusResponse Build(Func<string, string?> getEnvironmentVariable)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in DiagnosticEnvironmentVariableNames)
        {
            var raw = getEnvironmentVariable(name);
            vars[name] = string.IsNullOrEmpty(raw) ? "(not set)" : RedactedValue;
        }

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            EnvironmentVariables: vars);
    }
}
