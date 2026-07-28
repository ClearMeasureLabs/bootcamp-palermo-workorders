using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes configurable random data generation for scripts and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsRandomController(Random? random = null) : ControllerBase
{
    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Returns a random value as plain text for the requested <paramref name="type"/>.
    /// </summary>
    /// <param name="type">One of <c>number</c>, <c>string</c>, <c>uuid</c>, or <c>color</c> (case-insensitive).</param>
    /// <param name="min">Inclusive lower bound for <c>number</c> (default 0).</param>
    /// <param name="max">Exclusive upper bound for <c>number</c> (default 100).</param>
    /// <param name="length">Length for <c>string</c> (default 16, maximum 256).</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get(
        [FromQuery] string? type,
        [FromQuery] int? min,
        [FromQuery] int? max,
        [FromQuery] int? length)
    {
        if (string.IsNullOrWhiteSpace(type) || !RandomToolsGenerator.SupportedTypes.Contains(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required and must be one of: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        string content;
        switch (type.ToLowerInvariant())
        {
            case "number":
            {
                var resolvedMin = RandomToolsGenerator.ResolveMin(min);
                var resolvedMax = RandomToolsGenerator.ResolveMax(max);
                if (resolvedMin >= resolvedMax)
                {
                    return Problem(
                        detail: "For type 'number', 'min' must be less than 'max'.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                content = RandomToolsGenerator.GenerateNumber(_random, resolvedMin, resolvedMax);
                break;
            }

            case "string":
            {
                var resolvedLength = RandomToolsGenerator.ResolveStringLength(length);
                if (resolvedLength <= 0 || resolvedLength > RandomToolsGenerator.MaxStringLength)
                {
                    return Problem(
                        detail: $"For type 'string', 'length' must be between 1 and {RandomToolsGenerator.MaxStringLength}.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                content = RandomToolsGenerator.GenerateString(_random, resolvedLength);
                break;
            }

            case "uuid":
                content = Guid.NewGuid().ToString();
                break;

            case "color":
                content = RandomToolsGenerator.GenerateColor(_random);
                break;

            default:
                return Problem(
                    detail: "Query parameter 'type' is required and must be one of: number, string, uuid, color.",
                    statusCode: StatusCodes.Status400BadRequest);
        }

        return new ContentResult
        {
            Content = content,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
