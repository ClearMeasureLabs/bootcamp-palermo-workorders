using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.RateLimiting;

internal static class RateLimitingPipeline
{
    internal static async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next,
        IOptionsMonitor<ApiRateLimitingOptions> optionsMonitor,
        PartitionedRateLimiter<HttpContext> limiter)
    {
        if (!RateLimitingMiddlewareRules.ShouldApply(context))
        {
            await next(context);
            return;
        }

        var opts = optionsMonitor.CurrentValue;
        if (!opts.Enabled || !ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        var permitLease = await limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
        if (!permitLease.IsAcquired)
        {
            permitLease.Dispose();
            await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, opts);
            return;
        }

        try
        {
            RateLimitResponseWriter.AddRateLimitHeaders(context, limiter, opts);
            await next(context);
        }
        finally
        {
            permitLease.Dispose();
        }
    }
}
