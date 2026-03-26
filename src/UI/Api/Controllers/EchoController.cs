using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Diagnostic endpoint that echoes safe metadata from the current HTTP request.
/// </summary>
[ApiController]
public class EchoController(TimeProvider timeProvider) : ControllerBase
{
    private static readonly HashSet<string> AllowedHeaderNames =
    [
        HeaderNames.UserAgent,
        HeaderNames.Accept,
        HeaderNames.Host,
    ];

    /// <summary>
    /// Returns HTTP 200 with JSON describing the incoming request (method, path, query, allowlisted headers, remote IP, UTC timestamp).
    /// </summary>
    [HttpGet("/api/echo")]
    public ActionResult<EchoResponse> Get()
    {
        var queryParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query)
        {
            var values = pair.Value;
            queryParameters[pair.Key] = values.Count == 0
                ? string.Empty
                : values[^1]!;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllowedHeaderNames)
        {
            if (!Request.Headers.TryGetValue(name, out var values) || values.Count == 0)
            {
                continue;
            }

            headers[name] = values.ToString();
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;

        return Ok(new EchoResponse
        {
            Method = Request.Method,
            Path = Request.Path.Value ?? string.Empty,
            QueryParameters = queryParameters,
            Headers = headers,
            RemoteIp = remoteIp,
            Timestamp = timestamp,
        });
    }
}
