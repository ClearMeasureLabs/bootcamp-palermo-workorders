using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates one or more random GUIDs for integrations and tooling.
/// When <c>ApiKeyAuthentication:Enabled</c> is true, requests require the shared API key header like other protected <c>/api/*</c> routes (not in the version/time/ping public allowlist).
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
    /// Creates new GUIDs. Optional JSON body with <c>count</c> (default 1, maximum 100).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(GuidGeneratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GuidGeneratorRequest? request)
    {
        var count = request?.Count ?? DefaultCount;
        if (count < 1 || count > MaxCount)
        {
            return Problem(
                detail: $"Count must be between 1 and {MaxCount} inclusive.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var guids = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            guids.Add(Guid.NewGuid().ToString("D"));
        }

        return Ok(new GuidGeneratorResponse(guids));
    }
}

/// <summary>
/// Request body for <see cref="GuidGeneratorController.Post"/>.
/// </summary>
/// <param name="Count">Optional number of GUIDs to generate; omitted or null defaults to 1.</param>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// Response payload listing generated GUID strings in order.
/// </summary>
/// <param name="Guids">GUID strings in standard "D" format.</param>
public sealed record GuidGeneratorResponse(IReadOnlyList<string> Guids);
