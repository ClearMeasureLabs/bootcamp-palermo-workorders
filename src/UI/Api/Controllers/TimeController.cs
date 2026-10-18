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
    /// Returns the current UTC time and day of week.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var utcNow = timeProvider.GetUtcNow();
        var payload = new TimeResponse(
            Utc: utcNow.ToString("O", CultureInfo.InvariantCulture),
            DayOfWeek: utcNow.ToString("dddd", CultureInfo.InvariantCulture));
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/time</c> and <c>GET /api/v1.0/time</c>.
/// </summary>
/// <param name="Utc">Current UTC instant as ISO 8601.</param>
/// <param name="DayOfWeek">English name of the UTC calendar day (e.g. <c>Monday</c>).</param>
public record TimeResponse(string Utc, string DayOfWeek);
