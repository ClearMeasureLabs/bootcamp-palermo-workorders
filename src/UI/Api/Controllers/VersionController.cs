using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes build and deployment metadata for operators and integrations.
/// </summary>
[ApiController]
[Route("api/version")]
public class VersionController(IHostEnvironment hostEnvironment) : ControllerBase
{
    /// <summary>
    /// Returns assembly, runtime, and host metadata.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<VersionMetadataResponse> Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly.GetName().Version?.ToString();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return Ok(new VersionMetadataResponse(
            AssemblyVersion: assemblyVersion,
            InformationalVersion: informationalVersion,
            Environment: hostEnvironment.EnvironmentName,
            MachineName: Environment.MachineName,
            FrameworkDescription: RuntimeInformation.FrameworkDescription));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/version</c>.
/// </summary>
public record VersionMetadataResponse(
    string? AssemblyVersion,
    string? InformationalVersion,
    string? Environment,
    string MachineName,
    string FrameworkDescription);
