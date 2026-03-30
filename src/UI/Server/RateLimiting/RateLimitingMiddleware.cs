using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

/// <summary>
/// Enforces sliding-window limits for endpoints marked with <see cref="EnableRateLimitingAttribute"/> using
/// <see cref="ApiRateLimiting.PolicyName"/>, and adds standard rate-limit response headers.
/// </summary>
public sealed class RateLimitingMiddleware
{
    /// <summary>Response header: configured permit limit for the window.</summary>
    public const string HeaderLimit = "X-RateLimit-Limit";

    /// <summary>Response header: permits still available in the current window (approximate).</summary>
    public const string HeaderRemaining = "X-RateLimit-Remaining";

    /// <summary>Response header: Unix timestamp when the window fully resets.</summary>
    public const string HeaderReset = "X-RateLimit-Reset";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<ApiRateLimitingOptions> _optionsMonitor;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptionsMonitor<ApiRateLimitingOptions> optionsMonitor,
        PartitionedRateLimiter<HttpContext> limiter)
    {
        _next = next;
        _optionsMonitor = optionsMonitor;
        _limiter = limiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldApply(context))
        {
            await _next(context);
            return;
        }

        var opts = _optionsMonitor.CurrentValue;
        if (!opts.Enabled || !ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var permitLease = await _limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
        if (!permitLease.IsAcquired)
        {
            permitLease.Dispose();
            await WriteRateLimitedResponseAsync(context, opts);
            return;
        }

        try
        {
            AddRateLimitHeaders(context, opts);
            await _next(context);
        }
        finally
        {
            permitLease.Dispose();
        }
    }

    private static bool ShouldApply(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
            return false;

        var attr = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        return attr is { PolicyName: ApiRateLimiting.PolicyName };
    }

    private void AddRateLimitHeaders(HttpContext context, ApiRateLimitingOptions opts)
    {
        var stats = _limiter.GetStatistics(context);
        var remaining = stats?.CurrentAvailablePermits ?? 0;
        context.Response.Headers[HeaderLimit] = opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[HeaderRemaining] = Math.Max(0, remaining).ToString(NumberFormatInfo.InvariantInfo);
        var window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds));
        context.Response.Headers[HeaderReset] = DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds()
            .ToString(NumberFormatInfo.InvariantInfo);
    }

    private static async Task WriteRateLimitedResponseAsync(HttpContext context, ApiRateLimitingOptions opts)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retryAfter = Math.Max(1, opts.WindowSeconds);
        context.Response.Headers.RetryAfter = retryAfter.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[HeaderLimit] = opts.PermitLimit.ToString(NumberFormatInfo.InvariantInfo);
        context.Response.Headers[HeaderRemaining] = "0";
        var window = TimeSpan.FromSeconds(retryAfter);
        context.Response.Headers[HeaderReset] = DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds()
            .ToString(NumberFormatInfo.InvariantInfo);
        if (context.Features.Get<IHttpResponseFeature>()?.HasStarted != true)
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Too many requests. Please try again later.", context.RequestAborted);
        }
    }
}
