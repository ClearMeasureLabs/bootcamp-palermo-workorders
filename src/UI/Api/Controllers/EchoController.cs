using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Reflects key properties of the incoming HTTP request for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns a JSON object echoing method, path, query, scheme, host, protocol, and headers.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = Request;
        var payload = new EchoResponse(
            Method: request.Method,
            Path: request.Path.HasValue ? request.Path.Value! : string.Empty,
            QueryString: request.QueryString.HasValue ? request.QueryString.Value! : string.Empty,
            Query: ToQueryDictionary(request.Query),
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Protocol: request.Protocol,
            Headers: ToHeaderDictionary(request.Headers));
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static IReadOnlyDictionary<string, string?> ToQueryDictionary(IQueryCollection query)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
        {
            result[pair.Key] = pair.Value.Count == 0 ? null : pair.Value.ToString();
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> ToHeaderDictionary(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in headers)
        {
            result[pair.Key] = JoinHeaderValues(pair.Value);
        }

        return result;
    }

    private static string JoinHeaderValues(StringValues values) =>
        values.Count == 0 ? string.Empty : values.ToString();
}
