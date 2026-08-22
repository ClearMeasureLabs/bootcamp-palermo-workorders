namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and the versioned route.
/// </summary>
public sealed record TimestampConverterResponse(
    long EpochSeconds,
    long EpochMilliseconds,
    string Iso8601Utc,
    string UtcDisplay,
    string LocalDisplay);
