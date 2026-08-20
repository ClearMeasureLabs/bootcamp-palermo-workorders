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
public sealed class GuidGeneratorController : ControllerBase
{
    private const int MinCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Generates <paramref name="request"/>.<see cref="GuidGeneratorRequest.Count"/> GUIDs (default 1).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? 1;
        if (count < MinCount || count > MaxCount)
        {
            return Problem(
                detail: $"count must be between {MinCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[count];
        for (var i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return Ok(new GuidGeneratorResponse(guids));
    }
}
