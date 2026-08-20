using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Reflects key properties of the inbound HTTP request for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns JSON echoing method, path, query, scheme, host, protocol, and headers from the current request.
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
            Query: ToSafeDictionary(request.Query),
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Protocol: request.Protocol,
            Headers: ToSafeDictionary(request.Headers));
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static IReadOnlyDictionary<string, string> ToSafeDictionary(
        IEnumerable<KeyValuePair<string, StringValues>> pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pairs)
        {
            result[pair.Key] = pair.Value.ToString();
        }

        return result;
    }
}
