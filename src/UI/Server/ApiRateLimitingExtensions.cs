using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers sliding-window rate limiting for API HTTP endpoints.
/// </summary>
public static class ApiRateLimitingExtensions
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString BlazorSingleApiPath = new("/api/blazor-wasm-single-api");

    /// <summary>
    /// Adds rate limiter services using <see cref="ApiRateLimitingOptions"/> from configuration.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRateLimitingOptions>(configuration.GetSection(ApiRateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                }
                else
                {
                    var opts = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value;
                    var seconds = Math.Max(1, opts.WindowSeconds);
                    context.HttpContext.Response.Headers.RetryAfter =
                        seconds.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                }

                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken);
            };

            options.AddPolicy(ApiRateLimitingPolicyNames.ApiSlidingWindow, httpContext =>
            {
                var opts = httpContext.RequestServices.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value;
                if (!opts.Enabled || !ShouldApplyToPath(httpContext.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter(string.Empty);
                }

                var window = TimeSpan.FromSeconds(Math.Max(1, opts.WindowSeconds));
                var segments = Math.Max(1, opts.SegmentsPerWindow);
                var permitLimit = Math.Max(1, opts.PermitLimit);
                var partitionKey = ResolvePartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        SegmentsPerWindow = segments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Enables the configured rate limiting middleware. Call after <c>UseRouting</c>.
    /// </summary>
    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static bool ShouldApplyToPath(PathString path)
    {
        if (path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWithSegments(BlazorSingleApiPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        var userName = httpContext.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userName))
        {
            return "user:" + userName;
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            return "ip:" + remoteIp;
        }

        // TestServer and some proxies omit RemoteIpAddress; use one bucket so limits still apply.
        return "anonymous";
    }
}
