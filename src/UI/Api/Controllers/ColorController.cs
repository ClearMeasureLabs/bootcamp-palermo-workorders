using System.Security.Cryptography;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a random CSS hex color for operators, integrations, and end-to-end tests.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/color")]
[Route($"{ApiRoutes.VersionedApiPrefix}/color")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ColorController : ControllerBase
{
    /// <summary>
    /// Returns a random CSS hex color as plain text in the form <c>#RRGGBB</c> (uppercase hex digits).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("text/plain")]
    public IActionResult Get()
    {
        Span<byte> rgb = stackalloc byte[3];
        RandomNumberGenerator.Fill(rgb);
        var hex = Convert.ToHexString(rgb);
        return new ContentResult
        {
            Content = "#" + hex,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
