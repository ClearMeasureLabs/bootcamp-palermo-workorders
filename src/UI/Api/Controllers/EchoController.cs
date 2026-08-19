using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for operator and client-side debugging.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    private static readonly HashSet<string> AlwaysIncludedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "User-Agent",
        "Host",
        "X-Correlation-ID"
    };

    /// <summary>
    /// Returns JSON reflecting key properties of the incoming HTTP request.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = HttpContext.Request;
        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var payload = new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Query: query,
            Headers: CollectSafeHeaders(request.Headers));

        return ConditionalGetEtag.JsonContent(payload);
    }

    internal static Dictionary<string, string> CollectSafeHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (SensitiveHeaders.Contains(header.Key))
            {
                continue;
            }

            if (AlwaysIncludedHeaders.Contains(header.Key)
                || header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase))
            {
                result[header.Key] = header.Value.ToString();
            }
        }

        return result;
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public record EchoResponse(
    string Method,
    string Path,
    string PathBase,
    string QueryString,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers);
