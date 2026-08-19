using System.Globalization;
using System.Threading.RateLimiting;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Enforces sliding-window limits for endpoints marked with <see cref="EnableRateLimitingAttribute"/>.
/// </summary>
public sealed class RateLimitingMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiRateLimitingOptions> optionsMonitor,
    PartitionedRateLimiter<HttpContext> limiter)
{
    public const string HeaderLimit = "X-RateLimit-Limit";
    public const string HeaderRemaining = "X-RateLimit-Remaining";
    public const string HeaderReset = "X-RateLimit-Reset";

    public Task InvokeAsync(HttpContext context) =>
        RateLimitingPipeline.InvokeAsync(context, next, optionsMonitor, limiter);
}

internal static class RateLimitingMiddlewareRules
{
    internal static bool ShouldApply(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attr = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        return attr is { PolicyName: ApiRateLimiting.PolicyName };
    }
}

internal static class RateLimitResponseWriter
{
    internal static void AddRateLimitHeaders(
        HttpContext context,
        PartitionedRateLimiter<HttpContext> limiter,
        ApiRateLimitingOptions opts)
    {
        var stats = limiter.GetStatistics(context);
        var remaining = stats?.CurrentAvailablePermits ?? 0;
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit] =
            opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining] =
            Math.Max(0, remaining).ToString(NumberFormatInfo.InvariantInfo);
        var window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds));
        context.Response.Headers[RateLimitingMiddleware.HeaderReset] =
            DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);
    }

    internal static async Task WriteRateLimitedResponseAsync(HttpContext context, ApiRateLimitingOptions opts)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retryAfter = Math.Max(1, opts.WindowSeconds);
        context.Response.Headers.RetryAfter = retryAfter.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit] =
            opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining] = "0";
        var window = TimeSpan.FromSeconds(retryAfter);
        context.Response.Headers[RateLimitingMiddleware.HeaderReset] =
            DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds().ToString(NumberFormatInfo.InvariantInfo);
        if (context.Features.Get<IHttpResponseFeature>()?.HasStarted != true)
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Too many requests. Please try again later.", context.RequestAborted);
        }
    }
}
