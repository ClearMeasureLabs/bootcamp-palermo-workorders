namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON body for unix and ISO conversions plus readable UTC representations.
/// </summary>
public sealed record TimestampConverterResponse(
    long UnixEpochSeconds,
    long UnixEpochMilliseconds,
    string Iso8601Utc,
    string Rfc1123Utc,
    string UtcDisplay);
