using System.Globalization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes the current UTC instant for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/time")]
[Route($"{ApiRoutes.VersionedApiPrefix}/time")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimeController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the current UTC time as ISO 8601 plain text, or Unix seconds when format=unix.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string? format = null)
    {
        if (!string.IsNullOrEmpty(format))
        {
            if (!string.Equals(format, "unix", StringComparison.OrdinalIgnoreCase))
                return Problem(detail: "Unknown format. Supported values: unix.", statusCode: StatusCodes.Status400BadRequest);

            var unixText = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            return new ContentResult
            {
                Content = unixText,
                ContentType = "text/plain; charset=utf-8",
                StatusCode = StatusCodes.Status200OK
            };
        }

        var text = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        var etag = ConditionalGetEtag.CreateWeakEtagForPlainText(text);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return new ContentResult
        {
            Content = text,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
