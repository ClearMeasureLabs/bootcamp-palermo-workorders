using System.Reflection;
using System.Runtime.InteropServices;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes build and deployment metadata for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/version")]
[Route($"{ApiRoutes.VersionedApiPrefix}/version")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class VersionController(IHostEnvironment hostEnvironment) : ControllerBase
{
    /// <summary>
    /// Returns assembly, runtime, and host metadata from the application entry assembly when present (for example UI.Server), with a fallback to this assembly for unit-test and tool hosts.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = OutputCachePolicyNames.VersionMetadata)]
    public IActionResult Get()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(VersionController).Assembly;
        var assemblyVersion = assembly.GetName().Version?.ToString();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var buildConfiguration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        var payload = new VersionMetadataResponse(
            AssemblyVersion: assemblyVersion,
            InformationalVersion: informationalVersion,
            BuildConfiguration: buildConfiguration,
            Environment: hostEnvironment.EnvironmentName,
            MachineName: Environment.MachineName,
            FrameworkDescription: RuntimeInformation.FrameworkDescription);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/version</c> and <c>GET /api/v1.0/version</c>.
/// </summary>
/// <param name="AssemblyVersion">File/assembly version from the host entry assembly when available.</param>
/// <param name="InformationalVersion">Source control / informational version from the host entry assembly when available.</param>
/// <param name="BuildConfiguration">Whether this binary was built as Debug or Release.</param>
/// <param name="Environment">Current host environment name (for example Development, Production).</param>
/// <param name="MachineName">Machine name of the running host process.</param>
/// <param name="FrameworkDescription">.NET runtime description string.</param>
public record VersionMetadataResponse(
    string? AssemblyVersion,
    string? InformationalVersion,
    string BuildConfiguration,
    string? Environment,
    string MachineName,
    string FrameworkDescription);
