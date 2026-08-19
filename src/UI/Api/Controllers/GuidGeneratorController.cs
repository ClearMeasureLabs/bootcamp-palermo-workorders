using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more GUIDs on demand for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/guid-generator")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/guid-generator")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class GuidGeneratorController : ControllerBase
{
    private const int MinCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Generates new GUIDs. Optional JSON body <c>{ "count": N }</c> (default 1, max 100).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? MinCount;
        if (count < MinCount || count > MaxCount)
        {
            return Problem(
                detail: $"Count must be between {MinCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[count];
        for (var i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return Ok(new GuidGeneratorResponse(count, guids));
    }
}

/// <summary>
/// Optional request body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
public record GuidGeneratorRequest(int? Count);

/// <summary>
/// JSON payload returned by <c>POST /api/tools/guid-generator</c>.
/// </summary>
public record GuidGeneratorResponse(int Count, IReadOnlyList<string> Guids);
