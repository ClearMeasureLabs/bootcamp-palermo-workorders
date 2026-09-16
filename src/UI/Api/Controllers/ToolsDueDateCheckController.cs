using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Stateless date-check utility endpoint — returns the due-date urgency for a given date.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/due-date-check")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/due-date-check")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsDueDateCheckController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the <see cref="DueDateUrgency"/> for the supplied <paramref name="date"/> relative
    /// to today in America/Chicago. Assumes an open work order (Draft status) to produce
    /// meaningful urgency results.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(DueDateCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return Problem(
                detail: "Query parameter 'date' is required (format: YYYY-MM-DD).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return Problem(
                detail: $"Invalid date '{date}'. Expected format: YYYY-MM-DD (e.g. 2025-01-15).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var urgency = DueDateUrgencyCalculator.Calculate(
            parsedDate,
            WorkOrderStatus.Draft,
            timeProvider);

        return Ok(new DueDateCheckResponse(urgency.ToString()));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/due-date-check</c>.
/// </summary>
public record DueDateCheckResponse(string Urgency);
