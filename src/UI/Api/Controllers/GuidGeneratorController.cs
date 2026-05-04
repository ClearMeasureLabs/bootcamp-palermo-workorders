using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more RFC 4122 GUIDs for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/guid-generator")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/guid-generator")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class GuidGeneratorController : ControllerBase
{
    private const int DefaultCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Creates new GUIDs. Optional <paramref name="count"/> defaults to 1; maximum is 100.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] GuidGeneratorRequest? body, [FromQuery] int? count)
    {
        var resolved = body?.Count ?? count ?? DefaultCount;
        if (resolved < 1 || resolved > MaxCount)
        {
            return Problem(
                detail: $"Count must be between 1 and {MaxCount}, inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[resolved];
        for (var i = 0; i < resolved; i++)
            guids[i] = Guid.NewGuid().ToString("D");

        return Ok(new GuidGeneratorResponse(guids));
    }
}

/// <summary>
/// Optional JSON body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
public sealed record GuidGeneratorRequest(int? Count);

/// <summary>
/// JSON payload for GUID generator responses.
/// </summary>
public sealed record GuidGeneratorResponse(IReadOnlyList<string> Guids);
