using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for operator and developer diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController(TimeProvider timeProvider) : ControllerBase
{
    private const string CorrelationIdHttpContextItemKey = "CorrelationId";

    /// <summary>
    /// Returns JSON reflecting key properties of the incoming HTTP request.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var request = HttpContext.Request;
        string? correlationId = null;
        if (HttpContext.Items.TryGetValue(CorrelationIdHttpContextItemKey, out var item)
            && item is string correlationIdValue
            && !string.IsNullOrEmpty(correlationIdValue))
        {
            correlationId = correlationIdValue;
        }

        var payload = new EchoResponse(
            Method: request.Method,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Query: EchoRequestReflection.BuildQuery(request.Query),
            Headers: EchoRequestReflection.BuildHeaders(request.Headers),
            CorrelationId: correlationId,
            TimestampUtc: timeProvider.GetUtcNow());

        return ConditionalGetEtag.JsonContent(payload);
    }
}
