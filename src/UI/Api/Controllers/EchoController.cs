using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    private static readonly string[] AllowlistedHeaders =
    [
        "Accept",
        "User-Agent",
        "Host",
        "X-Correlation-Id",
        "X-Forwarded-For",
        "X-Forwarded-Proto"
    ];

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-API-Key"
    };

    /// <summary>
    /// Returns a JSON object reflecting key properties of the incoming HTTP request.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = HttpContext.Request;
        var path = (request.PathBase + request.Path).ToString();
        var query = request.Query.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Where(v => v is not null).Select(v => v!).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        var headers = BuildAllowlistedHeaders(request.Headers);

        var payload = new EchoResponse(
            Method: request.Method,
            Path: path,
            Query: query,
            Headers: headers);

        return ConditionalGetEtag.JsonContent(payload);
    }

    private static Dictionary<string, string> BuildAllowlistedHeaders(IHeaderDictionary requestHeaders)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllowlistedHeaders)
        {
            if (SensitiveHeaders.Contains(name))
            {
                continue;
            }

            if (requestHeaders.TryGetValue(name, out var values) && values.Count > 0)
            {
                result[name] = values.ToString();
            }
        }

        return result;
    }
}
