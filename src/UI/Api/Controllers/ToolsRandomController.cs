using System.Globalization;
using System.Security.Cryptography;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates random values for scripts and integrations. Query parameter <c>type</c> is required.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController : ControllerBase
{
    /// <summary>
    /// Length of the alphanumeric string returned when <paramref name="type"/> is <c>string</c>.
    /// </summary>
    public const int RandomStringLength = 16;

    private const string AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Returns random data as UTF-8 plain text. Supported <paramref name="type"/> values (case-insensitive):
    /// <c>number</c> — uniform random 32-bit signed integer; <c>string</c> — <see cref="RandomStringLength"/> alphanumeric characters;
    /// <c>uuid</c> — RFC 4122 version 4 GUID string; <c>color</c> — CSS hex <c>#RRGGBB</c> with uppercase hex digits.
    /// </summary>
    /// <param name="type">One of: number, string, uuid, color.</param>
    /// <returns>200 with <c>text/plain; charset=utf-8</c>, or 400 with problem details when <paramref name="type"/> is missing, empty, or unknown.</returns>
    [HttpGet]
    [AllowAnonymous]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Allowed values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var normalized = type.Trim();
        if (normalized.Equals("number", StringComparison.OrdinalIgnoreCase))
        {
            Span<byte> buf = stackalloc byte[4];
            RandomNumberGenerator.Fill(buf);
            var n = BitConverter.ToInt32(buf);
            return PlainText(n.ToString(CultureInfo.InvariantCulture));
        }

        if (normalized.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            var chars = new char[RandomStringLength];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = AlphanumericChars[RandomNumberGenerator.GetInt32(0, AlphanumericChars.Length)];
            }

            return PlainText(new string(chars));
        }

        if (normalized.Equals("uuid", StringComparison.OrdinalIgnoreCase))
        {
            Span<byte> uuidBytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(uuidBytes);
            uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x40);
            uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);
            var g = new Guid(uuidBytes);
            return PlainText(g.ToString("D"));
        }

        if (normalized.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            Span<byte> rgb = stackalloc byte[3];
            RandomNumberGenerator.Fill(rgb);
            var hex = Convert.ToHexString(rgb);
            return PlainText("#" + hex);
        }

        return Problem(
            detail: "Unknown 'type'. Allowed values: number, string, uuid, color.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static ContentResult PlainText(string content) =>
        new()
        {
            Content = content,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
}
