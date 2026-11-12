namespace ClearMeasure.Bootcamp.McpServer.RateLimiting;

/// <summary>
/// Configures MCP tool token-bucket limits and bounded client tracking.
/// </summary>
public sealed class McpToolRateLimitingOptions
{
    /// <summary>
    /// Configuration section containing MCP rate limit settings.
    /// </summary>
    public const string SectionName = "McpRateLimiting";

    /// <summary>
    /// Gets or sets the maximum number of client/tool buckets retained in memory.
    /// </summary>
    public int MaxTrackedBuckets { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the number of minutes a bucket may remain idle before eviction.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the limit used for tools without a specific configuration.
    /// </summary>
    public McpToolRateLimitOptions Default { get; set; } = new();

    /// <summary>
    /// Gets or sets per-tool limits, keyed by MCP tool name.
    /// </summary>
    public Dictionary<string, McpToolRateLimitOptions> Tools { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Describes a token-bucket capacity and refill schedule.
/// </summary>
public sealed class McpToolRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum burst of tool calls.
    /// </summary>
    public int Capacity { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of tokens replenished per period.
    /// </summary>
    public int RefillTokens { get; set; } = 10;

    /// <summary>
    /// Gets or sets the token refill period in seconds.
    /// </summary>
    public int RefillPeriodSeconds { get; set; } = 60;
}
