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
    /// Returns assembly, runtime, and host metadata.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = OutputCachePolicyNames.VersionMetadata)]
    public IActionResult Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly.GetName().Version?.ToString();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var payload = new VersionMetadataResponse(
            AssemblyVersion: assemblyVersion,
            InformationalVersion: informationalVersion,
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
public record VersionMetadataResponse(
    string? AssemblyVersion,
    string? InformationalVersion,
    string? Environment,
    string MachineName,
    string FrameworkDescription);
