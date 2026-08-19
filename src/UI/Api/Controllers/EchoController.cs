using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for operator and client debugging.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns JSON reflecting key properties of the incoming HTTP request.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = HttpContext.Request;
        var payload = EchoRequestReflection.Build(request);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Path,
    string PathBase,
    string? QueryString,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers);

internal static class EchoRequestReflection
{
    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    private static readonly HashSet<string> SafeHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "User-Agent",
        "Host",
        "X-Correlation-ID"
    };

    internal static EchoResponse Build(HttpRequest request)
    {
        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        return new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.HasValue ? request.QueryString.Value : null,
            Query: query,
            Headers: BuildSafeHeaders(request.Headers));
    }

    internal static Dictionary<string, string> BuildSafeHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (SensitiveHeaderNames.Contains(header.Key))
            {
                continue;
            }

            if (!ShouldIncludeHeader(header.Key))
            {
                continue;
            }

            result[header.Key] = header.Value.ToString();
        }

        return result;
    }

    private static bool ShouldIncludeHeader(string name)
    {
        if (SafeHeaderNames.Contains(name))
        {
            return true;
        }

        return name.StartsWith("X-", StringComparison.OrdinalIgnoreCase);
    }
}
