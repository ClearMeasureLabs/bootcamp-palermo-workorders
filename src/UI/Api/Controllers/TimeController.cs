using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes the current UTC instant for operators and integrations.
/// </summary>
[ApiController]
[Route("api/time")]
public class TimeController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the current UTC time as ISO 8601 plain text.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public ContentResult Get()
    {
        var text = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        return new ContentResult
        {
            Content = text,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = 200
        };
    }
}
