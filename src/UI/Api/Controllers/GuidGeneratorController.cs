using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more RFC 4122 GUIDs on demand for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/guid-generator")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/guid-generator")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class GuidGeneratorController : ControllerBase
{
    private const int MinCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Generates new GUID values. When omitted, <paramref name="request"/>.<c>Count</c> defaults to 1 (maximum 100).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? 1;

        if (count < MinCount || count > MaxCount)
        {
            return Problem(
                detail: "Count must be between 1 and 100.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            guids.Add(Guid.NewGuid().ToString());
        }

        return Ok(new GuidGeneratorResponse(count, guids));
    }
}
