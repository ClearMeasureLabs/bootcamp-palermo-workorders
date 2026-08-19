using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more new GUIDs for operators and integrations.
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
    /// Generates and returns new GUIDs. Optional JSON body: <c>{ "count": &lt;int&gt; }</c> (default 1, max 100).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        var count = 1;
        if (Request.ContentLength is > 0)
        {
            var request = await Request.ReadFromJsonAsync<GuidGeneratorRequest>(cancellationToken);
            count = request?.Count ?? 1;
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
