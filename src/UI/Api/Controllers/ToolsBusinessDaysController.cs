using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts Monday–Friday business days between two inclusive dates.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/business-days")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/business-days")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsBusinessDaysController : ControllerBase
{
    /// <summary>
    /// Returns the count of Monday–Friday days from <paramref name="from"/> to <paramref name="to"/> inclusive.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(BusinessDaysResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? from, [FromQuery] string? to)
    {
        if (!TryParseDate(from, "from", out var fromDate, out var fromError))
        {
            return Problem(detail: fromError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseDate(to, "to", out var toDate, out var toError))
        {
            return Problem(detail: toError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (fromDate > toDate)
        {
            return Problem(
                detail: "'from' must be on or before 'to'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new BusinessDaysResponse(CountBusinessDays(fromDate, toDate)));
    }

    private static bool TryParseDate(string? value, string name, out DateOnly date, out string error)
    {
        date = default;
        error = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"Query parameter '{name}' is required (format: YYYY-MM-DD).";
            return false;
        }

        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            error = $"Invalid date '{value}'. Expected format: YYYY-MM-DD (e.g. 2025-01-15).";
            return false;
        }

        return true;
    }

    private static int CountBusinessDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                count++;
            }
        }

        return count;
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/business-days</c>.
/// </summary>
public record BusinessDaysResponse(int BusinessDays);
