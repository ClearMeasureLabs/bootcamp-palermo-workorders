using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers partitioned sliding-window rate limiting used by <see cref="RateLimiting.RateLimitingMiddleware"/>.
/// </summary>
public static class ApiRateLimitingExtensions
{
    /// <summary>
    /// Binds <see cref="ApiRateLimitingOptions"/> and registers the shared <see cref="PartitionedRateLimiter{TResource}"/> for API routes.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRateLimitingOptions>(configuration.GetSection(ApiRateLimitingOptions.SectionName));
        services.AddSingleton(CreatePartitionedLimiter);
        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreatePartitionedLimiter(IServiceProvider sp)
    {
        var monitor = sp.GetRequiredService<IOptionsMonitor<ApiRateLimitingOptions>>();
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var opts = monitor.CurrentValue;
            if (!opts.Enabled || !ShouldApplyToPath(httpContext.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter(string.Empty);
            }

            var window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds));
            var partitionKey = ResolvePartitionKey(httpContext, opts.ApiKeyHeaderName);

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, opts.PermitLimit),
                    Window = window,
                    SegmentsPerWindow = Math.Max(1, opts.SegmentsPerWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = Math.Max(0, opts.QueueLimit),
                    AutoReplenishment = true
                });
        });
    }

    private static readonly PathString ApiPrefix = new("/api");

    private static readonly PathString BlazorSingleApiPath = new("/api/blazor-wasm-single-api");

    private static readonly PathString BlazorSingleApiPathV1 = new("/api/v1.0/blazor-wasm-single-api");

    internal static bool ShouldApplyToPath(PathString path)
    {
        if (path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWithSegments(BlazorSingleApiPath, StringComparison.OrdinalIgnoreCase))
            return true;

        return path.StartsWithSegments(BlazorSingleApiPathV1, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolvePartitionKey(HttpContext httpContext, string apiKeyHeaderName)
    {
        if (!string.IsNullOrWhiteSpace(apiKeyHeaderName)
            && httpContext.Request.Headers.TryGetValue(apiKeyHeaderName, out var keyValues))
        {
            var k = keyValues.ToString();
            if (!string.IsNullOrWhiteSpace(k))
                return "key:" + k;
        }

        var userName = httpContext.User.Identity?.Name;
        if (!string.IsNullOrEmpty(userName))
            return "user:" + userName;

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
            return "ip:" + remoteIp;

        return "anonymous";
    }
}
