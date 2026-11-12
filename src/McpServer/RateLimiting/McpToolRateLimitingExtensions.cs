using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.RateLimiting;

/// <summary>
/// Adds request filtering for configured MCP tool rate limits.
/// </summary>
public static class McpToolRateLimitingExtensions
{
    /// <summary>
    /// Adds per-session token-bucket rate limiting to MCP tool calls.
    /// </summary>
    public static IMcpServerBuilder WithMcpToolRateLimiting(
        this IMcpServerBuilder builder,
        IConfiguration configuration)
    {
        builder.Services.AddOptions<McpToolRateLimitingOptions>()
            .Bind(configuration.GetSection(McpToolRateLimitingOptions.SectionName))
            .Validate(IsValid, "MCP rate limit settings must use positive capacities, refill values, and timeouts.")
            .ValidateOnStart();
        builder.Services.AddSingleton<McpToolRateLimiter>();

        return builder.WithRequestFilters(filters =>
            filters.AddCallToolFilter(next => async (request, cancellationToken) =>
            {
                var server = request.Server!;
                var services = request.Services ?? server.Services!;
                var toolName = request.Params!.Name;
                var limiter = services.GetRequiredService<McpToolRateLimiter>();
                if (limiter.TryAcquire(server.SessionId ?? string.Empty, toolName, out var retryAfter))
                {
                    return await next(request, cancellationToken);
                }

                const string reason = "MCP tool rate limit exceeded.";
                var logger = services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ClearMeasure.Bootcamp.McpServer.RateLimiting");
                logger.LogInformation(
                    "MCP rate limit exceeded for tool {ToolName}; retry after {RetryAfterSeconds} seconds",
                    toolName,
                    Math.Ceiling(retryAfter.TotalSeconds));
                await services.GetRequiredService<IBus>()
                    .Publish(new AuditLogRecord(reason, toolName));

                return new CallToolResult
                {
                    IsError = true,
                    Content =
                    [
                        new TextContentBlock
                        {
                            Text = $"{reason} Retry after {Math.Ceiling(retryAfter.TotalSeconds)} seconds."
                        }
                    ]
                };
            }));
    }

    private static bool IsValid(McpToolRateLimitingOptions options) =>
        options.MaxTrackedBuckets > 0
        && options.IdleTimeoutMinutes > 0
        && IsValid(options.Default)
        && options.Tools.Values.All(IsValid);

    private static bool IsValid(McpToolRateLimitOptions options) =>
        options.Capacity > 0 && options.RefillTokens > 0 && options.RefillPeriodSeconds > 0;
}
