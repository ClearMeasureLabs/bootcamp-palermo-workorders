using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more new GUIDs on demand for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/guid-generator")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/guid-generator")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class GuidGeneratorController : ControllerBase
{
    private const int DefaultCount = 1;
    private const int MinCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Returns a JSON array of newly generated GUID strings in <c>"D"</c> format.
    /// Optional query parameter <c>count</c> defaults to 1 and must be between 1 and 100 inclusive.
    /// </summary>
    /// <param name="count">Number of GUIDs to generate (default 1, maximum 100).</param>
    [HttpPost]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromQuery] int? count = null)
    {
        var requested = count ?? DefaultCount;
        if (requested < MinCount || requested > MaxCount)
        {
            return Problem(
                detail: $"count must be between {MinCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[requested];
        for (var i = 0; i < requested; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return Ok(guids);
    }
}
