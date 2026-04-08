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
/// Generates one or more new GUIDs for scripts, configs, and integrations.
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
    /// Creates new random GUIDs. Optional query <paramref name="count"/> defaults to 1; must be between 1 and 100 inclusive.
    /// </summary>
    /// <param name="count">Number of GUIDs to generate.</param>
    [HttpPost]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromQuery] int count = 1)
    {
        if (ModelState.GetValidationState("count") == ModelValidationState.Invalid)
        {
            return Problem(
                detail: $"Query parameter count must be an integer between {MinCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

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
