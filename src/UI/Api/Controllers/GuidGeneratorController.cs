using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more RFC 4122 GUIDs for scripts and integrations.
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
    /// Creates new GUIDs. Optional JSON body: <c>{"count": N}</c> where N is from 1 to 100 (default 1).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? DefaultCount;
        if (count < 1 || count > MaxCount)
        {
            return Problem(
                detail: $"count must be between 1 and {MaxCount}, inclusive.",
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

/// <summary>
/// Request body for <see cref="GuidGeneratorController"/>.
/// </summary>
public sealed class GuidGeneratorRequest
{
    /// <summary>
    /// Number of GUIDs to generate (1–100). When omitted, one GUID is returned.
    /// </summary>
    public int? Count { get; set; }
}

/// <summary>
/// Response payload for <see cref="GuidGeneratorController"/>.
/// </summary>
/// <param name="Guids">Newly generated GUID strings (standard "D" format).</param>
public sealed record GuidGeneratorResponse(IReadOnlyList<string> Guids);
