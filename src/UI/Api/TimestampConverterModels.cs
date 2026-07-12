namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and the versioned route.
/// </summary>
public sealed record TimestampConverterResponse(
    string InputKind,
    long EpochSeconds,
    long EpochMilliseconds,
    string Iso8601Utc,
    string Utc,
    string Local,
    string LocalTimeZoneId);

/// <summary>
/// Outcome of a timestamp conversion attempt.
/// </summary>
public sealed record TimestampConversionResult(
    bool Success,
    TimestampConverterResponse? Payload,
    string? ErrorDetail)
{
    /// <summary>
    /// Creates a successful conversion result.
    /// </summary>
    public static TimestampConversionResult Ok(TimestampConverterResponse payload) =>
        new(true, payload, null);

    /// <summary>
    /// Creates a failed conversion result with a client-facing error detail.
    /// </summary>
    public static TimestampConversionResult Fail(string errorDetail) =>
        new(false, null, errorDetail);
}
