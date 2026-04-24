using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and the versioned route.
/// </summary>
/// <param name="UnixTimeSeconds">Unix epoch seconds (UTC).</param>
/// <param name="UnixTimeMilliseconds">Unix epoch milliseconds (UTC).</param>
/// <param name="Iso8601Utc">ISO-8601 round-trip string for the instant in UTC (<c>O</c> format).</param>
/// <param name="UtcDate">Calendar date in UTC (<c>yyyy-MM-dd</c>).</param>
/// <param name="UtcTimeOfDay">Time-of-day in UTC (<c>HH:mm:ss.fff</c>).</param>
/// <param name="DisplayUtc">Single human-readable UTC label using invariant culture.</param>
public record TimestampConverterResponse(
    long UnixTimeSeconds,
    long UnixTimeMilliseconds,
    string Iso8601Utc,
    string UtcDate,
    string UtcTimeOfDay,
    string DisplayUtc)
{
    /// <summary>
    /// Builds a response from a UTC-normalized <see cref="DateTimeOffset"/>.
    /// </summary>
    public static TimestampConverterResponse FromUtc(DateTimeOffset utc)
    {
        var normalized = utc.ToOffset(TimeSpan.Zero);
        var dt = normalized.UtcDateTime;
        return new TimestampConverterResponse(
            UnixTimeSeconds: normalized.ToUnixTimeSeconds(),
            UnixTimeMilliseconds: normalized.ToUnixTimeMilliseconds(),
            Iso8601Utc: normalized.ToString("O", CultureInfo.InvariantCulture),
            UtcDate: dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            UtcTimeOfDay: dt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            DisplayUtc: dt.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture));
    }
}
