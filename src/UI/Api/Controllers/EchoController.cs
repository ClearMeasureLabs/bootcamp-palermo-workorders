using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON snapshot of the incoming HTTP request for client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns a JSON object reflecting key properties of the incoming HTTP request.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = HttpContext.Request;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = EchoHeaderRedaction.IsSensitive(header.Key)
                ? EchoHeaderRedaction.RedactedValue
                : header.Value.ToString();
        }

        IReadOnlyDictionary<string, string>? query = null;
        if (request.Query.Count > 0)
        {
            query = request.Query.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
        }

        string? correlationId = null;
        if (HttpContext.Items.TryGetValue("CorrelationId", out var item) && item is string id)
        {
            correlationId = id;
        }

        var payload = new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Protocol: request.Protocol,
            RemoteIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            Headers: headers,
            Query: query,
            CorrelationId: correlationId);

        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    string Scheme,
    string Host,
    string Protocol,
    string? RemoteIpAddress,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string>? Query,
    string? CorrelationId = null);
