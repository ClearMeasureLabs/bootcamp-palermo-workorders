using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates non-cryptographic random samples for scripts and integrations. Do not use outputs as secrets.
/// String values use <see cref="Random"/> over the alphanumeric ASCII alphabet (length 24).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController(Random? random = null) : ControllerBase
{
    private const int AlphanumericLength = 24;
    private const string AlphanumericAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Returns a random value according to <paramref name="type"/>:
    /// <c>number</c> (32-bit signed int), <c>string</c> (alphanumeric), <c>uuid</c>, or <c>color</c> (#RRGGBB).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Use number, string, uuid, or color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var normalized = type.Trim();
        ToolsRandomResponse? payload = null;

        if (string.Equals(normalized, "number", StringComparison.OrdinalIgnoreCase))
        {
            payload = new ToolsRandomResponse("number", _random.Next());
        }
        else if (string.Equals(normalized, "string", StringComparison.OrdinalIgnoreCase))
        {
            payload = new ToolsRandomResponse("string", NextAlphanumeric(_random, AlphanumericLength));
        }
        else if (string.Equals(normalized, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            payload = new ToolsRandomResponse("uuid", Guid.NewGuid().ToString("D"));
        }
        else if (string.Equals(normalized, "color", StringComparison.OrdinalIgnoreCase))
        {
            payload = new ToolsRandomResponse("color", NextCssHexColor(_random));
        }

        if (payload == null)
        {
            return Problem(
                detail: $"Unsupported type '{type}'. Use number, string, uuid, or color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static string NextAlphanumeric(Random random, int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(AlphanumericAlphabet[random.Next(AlphanumericAlphabet.Length)]);
        }

        return sb.ToString();
    }

    private static string NextCssHexColor(Random random) =>
        string.Create(7, random.Next(0, 0x1000000), static (chars, rgb) =>
        {
            chars[0] = '#';
            _ = rgb.TryFormat(chars[1..], out _, "x6");
        });

    /// <summary>
    /// JSON body for <c>GET /api/tools/random</c>. <see cref="Value"/> is a number for type <c>number</c> or a string otherwise.
    /// </summary>
    public sealed record ToolsRandomResponse(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("value")] object Value);
}
