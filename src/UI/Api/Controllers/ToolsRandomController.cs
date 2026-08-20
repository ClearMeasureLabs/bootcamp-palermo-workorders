using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates random sample values for integrations and tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController : ControllerBase
{
    private const string AlphanumericCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private const int DefaultStringLength = 12;

    /// <summary>
    /// Returns a random value for the requested <paramref name="type"/>.
    /// Supported types: <c>number</c>, <c>string</c>, <c>uuid</c>, <c>color</c>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ToolsRandomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Supported values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var normalized = type.Trim().ToLowerInvariant();
        var value = GenerateValue(normalized);
        if (value is null)
        {
            return Problem(
                detail: $"Unknown type '{type}'. Supported values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new ToolsRandomResponse(normalized, value));
    }

    private static string? GenerateValue(string type) =>
        type switch
        {
            "number" => Random.Shared.Next().ToString(),
            "string" => GenerateAlphanumericString(DefaultStringLength),
            "uuid" => Guid.NewGuid().ToString(),
            "color" => $"#{Random.Shared.Next(0x1000000):X6}",
            _ => null
        };

    private static string GenerateAlphanumericString(int length)
    {
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = AlphanumericCharacters[Random.Shared.Next(AlphanumericCharacters.Length)];
        }

        return new string(buffer);
    }
}

/// <summary>
/// JSON envelope for <see cref="ToolsRandomController"/>.
/// </summary>
/// <param name="Type">Requested generation type.</param>
/// <param name="Value">Generated value as a string.</param>
public sealed record ToolsRandomResponse(string Type, string Value);
