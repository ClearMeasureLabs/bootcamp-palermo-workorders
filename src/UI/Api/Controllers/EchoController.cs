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
    /// <summary>
    /// Returns JSON reflecting key properties of the incoming HTTP request (method, path, query, selected headers).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = HttpContext.Request;

        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in request.Query)
        {
            query[kvp.Key] = kvp.Value.ToString();
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (ShouldIncludeHeader(header.Key))
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        var payload = new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Query: query,
            Headers: headers);

        return ConditionalGetEtag.JsonContent(payload);
    }

    internal static bool ShouldIncludeHeader(string name)
    {
        if (IsSensitiveHeader(name))
        {
            return false;
        }

        if (name.Equals("Accept", StringComparison.OrdinalIgnoreCase)
            || name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Host", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return name.StartsWith("X-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSensitiveHeader(string name) =>
        name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
        || name.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase);
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
