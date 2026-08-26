using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more GUIDs for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/guid-generator")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/guid-generator")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsGuidGeneratorController : ControllerBase
{
    private const int DefaultCount = 1;
    private const int MaxCount = 100;

    /// <summary>
    /// Returns a JSON array of newly generated GUIDs in D format.
    /// Optional <paramref name="count"/> defaults to 1 and must be between 1 and 100 inclusive.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromQuery] int count = DefaultCount)
    {
        if (count < DefaultCount || count > MaxCount)
        {
            return Problem(
                detail: $"Query parameter 'count' must be between {DefaultCount} and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new string[count];
        for (var i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid().ToString("D");
        }

        return Ok(guids);
    }
}
