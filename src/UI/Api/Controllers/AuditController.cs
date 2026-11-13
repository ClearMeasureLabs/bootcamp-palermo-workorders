using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>Exposes bounded audit history to separately authorized administrators.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/audit")]
[Route($"{ApiRoutes.VersionedApiPrefix}/audit")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class AuditController(IAuditLog audit, IConfiguration configuration) : ControllerBase
{
    /// <summary>Reads up to 200 records, newest first, using a sequence cursor and optional UTC lower bound.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? from = null,
        [FromQuery] long? beforeId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdministrator())
            return StatusCode(StatusCodes.Status403Forbidden);

        if (!TryParseFrom(from, out var fromUtc))
            return Problem(detail: "from must be a UTC date (yyyy-MM-dd) or timestamp ending in Z.", statusCode: 400);

        if (!AreFiltersValid(entityType, beforeId))
            return Problem(detail: "Use a positive beforeId and an entityType of 1–255 characters.", statusCode: 400);

        Response.Headers.CacheControl = "no-store";
        return Ok(await audit.ReadAsync(entityType, entityId, fromUtc, beforeId, cancellationToken));
    }

    private static bool AreFiltersValid(string? entityType, long? beforeId) =>
        beforeId is not <= 0 &&
        (entityType is null || (entityType.Length <= 255 && !string.IsNullOrWhiteSpace(entityType)));

    private static bool TryParseFrom(string? from, out DateTime? fromUtc)
    {
        fromUtc = null;
        if (from is null)
            return true;

        if (DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            fromUtc = date;
            return true;
        }

        if (!from.EndsWith('Z') || !DateTimeOffset.TryParse(from, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) || parsed.Offset != TimeSpan.Zero)
            return false;
        fromUtc = parsed.UtcDateTime;
        return true;
    }

    private bool IsAdministrator() => IsAuthenticatedAdministrator() || HasAdministratorKey();

    private bool IsAuthenticatedAdministrator() =>
        User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

    private bool HasAdministratorKey()
    {
        var expected = configuration["AuditLog:AdministratorApiKey"];
        var supplied = Request.Headers["X-Audit-Api-Key"];
        if (string.IsNullOrWhiteSpace(expected) || supplied.Count != 1 || string.IsNullOrEmpty(supplied[0])
            || supplied[0]!.Length > 4096)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!)));
    }
}
