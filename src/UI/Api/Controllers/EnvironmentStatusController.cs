using System.Reflection;
using System.Runtime.InteropServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

// Runtime environment snapshot for operations and support. Environment variable
// values are never returned.
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController : ControllerBase
{
    public const string RedactedValue = EchoController.RedactedValue;
    public const string RedactionProbeVariableName = "TEST_ENV_STATUS_SECRET";

    private static readonly string[] ReportedEnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "DOTNET_ENVIRONMENT",
        "DOTNET_ROOT",
        "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",
        RedactionProbeVariableName
    ];

    [HttpGet]
    public IActionResult Get()
    {
        var payload = BuildResponse();
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static EnvironmentStatusResponse BuildResponse()
    {
        var names = new List<string>();
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in ReportedEnvironmentVariableNames)
        {
            if (Environment.GetEnvironmentVariable(name) is null)
            {
                continue;
            }

            names.Add(name);
            variables[name] = RedactedValue;
        }

        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
        var gitSha = Assembly.GetEntryAssembly()
            ?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "SourceRevisionId")
            ?.Value ?? "unknown";
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "unknown";

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            EnvironmentVariableNames: names,
            EnvironmentVariables: variables,
            Version: version,
            GitSha: gitSha,
            EnvironmentName: environmentName);
    }
}

public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyList<string> EnvironmentVariableNames,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    string Version,
    string GitSha,
    string EnvironmentName);
