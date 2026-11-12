using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.McpServer.RateLimiting;

/// <summary>
/// Enforces token-bucket limits per MCP client session and tool.
/// </summary>
public sealed class McpToolRateLimiter(
    IOptions<McpToolRateLimitingOptions> options,
    TimeProvider timeProvider)
{
    private readonly object _gate = new();
    private readonly Dictionary<BucketKey, Bucket> _buckets = [];
    private readonly McpToolRateLimitingOptions _options = options.Value;
    private DateTimeOffset _nextCleanup;

    /// <summary>
    /// Attempts to consume one token from a client's bucket for the named tool.
    /// </summary>
    public bool TryAcquire(string clientSessionId, string toolName, out TimeSpan retryAfter)
    {
        var now = timeProvider.GetUtcNow();
        var configuredTool = FindToolLimit(toolName, out var limit);
        var key = new BucketKey(HashClientSession(clientSessionId), configuredTool);

        lock (_gate)
        {
            RemoveIdleBuckets(now);
            if (!_buckets.TryGetValue(key, out var bucket))
            {
                if (_buckets.Count >= _options.MaxTrackedBuckets)
                {
                    retryAfter = TimeSpan.FromMinutes(_options.IdleTimeoutMinutes);
                    return false;
                }

                bucket = new Bucket(limit.Capacity, now);
                _buckets.Add(key, bucket);
            }

            bucket.Refill(now, limit);
            bucket.LastAccess = now;
            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
                retryAfter = TimeSpan.Zero;
                return true;
            }

            retryAfter = TimeSpan.FromSeconds(Math.Max(
                1,
                (int)Math.Ceiling((1 - bucket.Tokens) * limit.RefillPeriodSeconds / limit.RefillTokens)));
            return false;
        }
    }

    private string FindToolLimit(string toolName, out McpToolRateLimitOptions limit)
    {
        foreach (var configuredTool in _options.Tools)
        {
            if (string.Equals(configuredTool.Key, toolName, StringComparison.OrdinalIgnoreCase))
            {
                limit = configuredTool.Value;
                return configuredTool.Key;
            }
        }

        limit = _options.Default;
        return string.Empty;
    }

    private void RemoveIdleBuckets(DateTimeOffset now)
    {
        if (now < _nextCleanup)
        {
            return;
        }

        var idleLimit = TimeSpan.FromMinutes(_options.IdleTimeoutMinutes);
        foreach (var key in _buckets
                     .Where(pair => now - pair.Value.LastAccess >= idleLimit)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _buckets.Remove(key);
        }

        _nextCleanup = now.AddMinutes(1);
    }

    private static string HashClientSession(string clientSessionId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientSessionId ?? string.Empty)));

    private readonly record struct BucketKey(string ClientSessionHash, string ToolName);

    private sealed class Bucket(double capacity, DateTimeOffset now)
    {
        public double Tokens { get; set; } = capacity;

        public DateTimeOffset LastAccess { get; set; } = now;

        private DateTimeOffset LastRefill { get; set; } = now;

        public void Refill(DateTimeOffset now, McpToolRateLimitOptions limit)
        {
            var elapsedSeconds = Math.Max(0, (now - LastRefill).TotalSeconds);
            Tokens = Math.Min(
                limit.Capacity,
                Tokens + elapsedSeconds * limit.RefillTokens / limit.RefillPeriodSeconds);
            LastRefill = now;
        }
    }
}
