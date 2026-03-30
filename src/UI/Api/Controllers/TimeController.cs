using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes the current UTC instant for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/time")]
[Route($"{ApiRoutes.VersionedApiPrefix}/time")]
public class TimeController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the current UTC time as ISO 8601 plain text.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
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
