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
    /// Generates new GUIDs. Optional <paramref name="count"/> defaults to 1 (valid range 1–100).
    /// </summary>
    /// <param name="request">Optional JSON body with a <c>count</c> property.</param>
    /// <param name="count">Optional query-string override for the number of GUIDs to generate.</param>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult Post([FromBody] GuidGeneratorRequest? request, [FromQuery] int? count)
    {
        var resolvedCount = count ?? request?.Count ?? 1;

        if (resolvedCount < MinCount || resolvedCount > MaxCount)
        {
            return Problem(
                detail: $"count must be between {MinCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[resolvedCount];
        for (var i = 0; i < resolvedCount; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return Ok(new GuidGeneratorResponse(guids));
    }
}

/// <summary>
/// JSON request body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Count">Number of GUIDs to generate (1–100). Defaults to 1 when omitted.</param>
public record GuidGeneratorRequest(int? Count);

/// <summary>
/// JSON payload returned by <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Guids">Generated GUID strings in lowercase hyphenated format.</param>
public record GuidGeneratorResponse(IReadOnlyList<string> Guids);
