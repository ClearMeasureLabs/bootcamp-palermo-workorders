namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Configuration for <see cref="IdempotencyMiddleware"/>.
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// How long a successful response snapshot is retained for replay, in seconds. Default 24 hours.
    /// </summary>
    public int CacheEntrySeconds { get; set; } = 86400;

    /// <summary>
    /// Maximum length of the idempotency key header value (after trim). Longer values yield 400 Bad Request.
    /// </summary>
    public int MaxKeyLength { get; set; } = 256;
}
