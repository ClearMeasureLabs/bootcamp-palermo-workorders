using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Utility endpoints for generating sample random values (machine clients).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController(IRandomToolPayloadGenerator generator) : ControllerBase
{
    /// <summary>
    /// Returns a JSON object with <c>type</c> and <c>value</c> for the requested generator.
    /// Query <c>type</c>: <c>number</c> (32-bit signed int), <c>string</c> (24 alphanumeric chars),
    /// <c>uuid</c> (GUID string), <c>color</c> (CSS <c>#rrggbb</c> lowercase).
    /// </summary>
    [HttpGet("random")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RandomToolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetRandom([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Supported values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var normalized = type.Trim();
        if (string.Equals(normalized, "number", StringComparison.OrdinalIgnoreCase))
        {
            var value = generator.NextInt32();
            return OkJson(new RandomToolResponse("number", value));
        }

        if (string.Equals(normalized, "string", StringComparison.OrdinalIgnoreCase))
        {
            const int stringLength = 24;
            var value = generator.NextAlphanumericString(stringLength);
            return OkJson(new RandomToolResponse("string", value));
        }

        if (string.Equals(normalized, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            var value = generator.NextGuid().ToString("D");
            return OkJson(new RandomToolResponse("uuid", value));
        }

        if (string.Equals(normalized, "color", StringComparison.OrdinalIgnoreCase))
        {
            var value = generator.NextCssHexColor();
            return OkJson(new RandomToolResponse("color", value));
        }

        return Problem(
            detail: $"Unsupported type '{normalized}'. Supported values: number, string, uuid, color.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult OkJson(RandomToolResponse payload)
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON body for <c>GET /api/tools/random</c>.
/// </summary>
/// <param name="Type">Echo of the requested generator kind.</param>
/// <param name="Value">Generated value (number, string, uuid, or hex color).</param>
public record RandomToolResponse(string Type, object Value);
