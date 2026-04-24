using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more new GUID strings for scripts and integrations.
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
    /// Creates new GUID values. Request body is optional; when omitted or when <see cref="GuidGeneratorRequest.Count"/> is null, one GUID is returned.
    /// </summary>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? DefaultCount;
        if (count < 1 || count > MaxCount)
        {
            return Problem(
                detail: $"Count must be between 1 and {MaxCount}, inclusive.",
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
/// Optional JSON body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Count">Number of GUIDs to generate; defaults to 1 when omitted or null.</param>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// Response payload containing newly generated GUID strings (lowercase hex with hyphens, "D" format).
/// </summary>
/// <param name="Guids">The generated identifiers.</param>
public sealed record GuidGeneratorResponse(IReadOnlyList<string> Guids);
