using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/diagnostics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/diagnostics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class DiagnosticsController(
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider,
    IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        var payload = new DiagnosticsResponse(
            Environment: hostEnvironment.EnvironmentName,
            Uptime: healthSlice.Uptime,
            FeatureFlags: featureFlagsOptions.Value);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
