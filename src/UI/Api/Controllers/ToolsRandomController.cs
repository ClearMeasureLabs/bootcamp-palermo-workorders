using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns configurable pseudo-random data for tests, demos, and tooling (no cryptographic guarantees).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController : ControllerBase
{
    private static readonly Random RandomSource = Random.Shared;

    /// <summary>
    /// Generates a random value according to <paramref name="kind"/> and query parameters.
    /// </summary>
    /// <param name="kind">
    /// <c>int</c> (default), <c>long</c>, <c>guid</c>, <c>string</c>, or <c>bytes</c>.
    /// </param>
    /// <param name="min">Inclusive lower bound for <c>int</c> / <c>long</c> (defaults apply when omitted).</param>
    /// <param name="max">Exclusive upper bound for <c>int</c> / <c>long</c>.</param>
    /// <param name="length">Length for <c>string</c> or <c>bytes</c> (default 16, max 4096).</param>
    /// <param name="charset">Character set for <c>string</c>; defaults to alphanumeric ASCII.</param>
    /// <param name="encoding"><c>base64</c> or <c>hex</c> for <c>bytes</c> output.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get(
        [FromQuery] string? kind = null,
        [FromQuery] string? min = null,
        [FromQuery] string? max = null,
        [FromQuery] string? length = null,
        [FromQuery] string? charset = null,
        [FromQuery] string? encoding = null)
    {
        if (!ToolsRandomGenerator.TryParseKind(kind, out var parsedKind))
            return Problem(detail: $"Unknown kind '{kind}'. Supported: int, long, guid, string, bytes.", statusCode: StatusCodes.Status400BadRequest);

        return parsedKind switch
        {
            RandomKind.Int => GetInt(min, max),
            RandomKind.Long => GetLong(min, max),
            RandomKind.Guid => OkJson(new { kind = "guid", value = ToolsRandomGenerator.NextGuid(RandomSource).ToString("D") }),
            RandomKind.String => GetString(length, charset),
            RandomKind.Bytes => GetBytes(length, encoding),
            _ => Problem(detail: "Unsupported kind.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private IActionResult GetInt(string? minRaw, string? maxRaw)
    {
        if (!ToolsRandomGenerator.TryParseIntBounds(minRaw, maxRaw, out var minInclusive, out var maxExclusive, out var error))
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);

        var value = ToolsRandomGenerator.NextInt(RandomSource, minInclusive, maxExclusive);
        return OkJson(new
        {
            kind = "int",
            minInclusive,
            maxExclusive,
            value
        });
    }

    private IActionResult GetLong(string? minRaw, string? maxRaw)
    {
        if (!ToolsRandomGenerator.TryParseLongBounds(minRaw, maxRaw, out var minInclusive, out var maxExclusive, out var error))
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);

        var value = ToolsRandomGenerator.NextLong(RandomSource, minInclusive, maxExclusive);
        return OkJson(new
        {
            kind = "long",
            minInclusive,
            maxExclusive,
            value
        });
    }

    private IActionResult GetString(string? lengthRaw, string? charsetRaw)
    {
        if (!ToolsRandomGenerator.TryParseBoundedInt(
                lengthRaw,
                0,
                ToolsRandomGenerator.MaxStringOrByteLength,
                "length",
                out var strLength,
                out var lenError))
            return Problem(detail: lenError, statusCode: StatusCodes.Status400BadRequest);

        if (!ToolsRandomGenerator.TryValidateCharset(charsetRaw, out var charset, out var charsetError))
            return Problem(detail: charsetError, statusCode: StatusCodes.Status400BadRequest);

        var value = ToolsRandomGenerator.NextString(RandomSource, strLength, charset);
        return OkJson(new
        {
            kind = "string",
            length = strLength,
            value
        });
    }

    private IActionResult GetBytes(string? lengthRaw, string? encodingRaw)
    {
        if (!ToolsRandomGenerator.TryParseBoundedInt(
                lengthRaw,
                0,
                ToolsRandomGenerator.MaxStringOrByteLength,
                "length",
                out var byteLength,
                out var lenError))
            return Problem(detail: lenError, statusCode: StatusCodes.Status400BadRequest);

        if (!ToolsRandomGenerator.TryParseBytesEncoding(encodingRaw, out var enc))
            return Problem(detail: $"Unknown encoding '{encodingRaw}'. Supported: base64, hex.", statusCode: StatusCodes.Status400BadRequest);

        var value = ToolsRandomGenerator.NextBytesEncoded(RandomSource, byteLength, enc);
        return OkJson(new
        {
            kind = "bytes",
            length = byteLength,
            encoding = enc.ToString().ToLowerInvariant(),
            value
        });
    }

    private static ContentResult OkJson(object payload) => ConditionalGetEtag.JsonContent(payload);
}
