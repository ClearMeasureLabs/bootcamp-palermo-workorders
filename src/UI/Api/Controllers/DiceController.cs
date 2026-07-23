using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a plain-text six-sided die roll for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/dice")]
[Route($"{ApiRoutes.VersionedApiPrefix}/dice")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class DiceController : ControllerBase
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiceController"/> class.
    /// </summary>
    /// <param name="random">Optional random source; defaults to <see cref="Random.Shared"/>.</param>
    public DiceController(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Returns a random integer from 1 through 6 as plain text.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var roll = _random.Next(1, 7);
        return new ContentResult
        {
            Content = roll.ToString(),
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
