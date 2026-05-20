using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more RFC 4122 GUIDs for API clients.
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
    /// Creates new GUIDs. Optional <c>count</c> in the query string (1–100, default 1) takes precedence
    /// over an optional JSON body <c>{"count": n}</c>.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromQuery] int? count, [FromBody] GuidGeneratorRequest? body)
    {
        var resolved = count ?? body?.Count;
        if (!resolved.HasValue)
            resolved = MinCount;

        var c = resolved.Value;
        if (c < MinCount || c > MaxCount)
        {
            return Problem(
                detail: $"count must be between {MinCount} and {MaxCount} (inclusive).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[c];
        for (var i = 0; i < c; i++)
            guids[i] = Guid.NewGuid().ToString("D");

        return Ok(new GuidGeneratorResponse(c, guids));
    }
}

/// <summary>
/// Optional JSON body for POST <c>/api/tools/guid-generator</c>.
/// </summary>
public sealed record GuidGeneratorRequest(int? Count);

/// <summary>
/// Response payload listing generated GUIDs in the standard 32-digit hyphenated format.
/// </summary>
public sealed record GuidGeneratorResponse(int Count, IReadOnlyList<string> Guids);
