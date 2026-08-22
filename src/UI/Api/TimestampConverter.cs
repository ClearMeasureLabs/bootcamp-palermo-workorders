using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses epoch or ISO-8601 inputs and builds canonical timestamp representations.
/// </summary>
public static class TimestampConverter
{
    private const long MillisecondsThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Parses a Unix epoch value, auto-detecting seconds versus milliseconds.
    /// </summary>
    /// <param name="epochText">Numeric epoch string.</param>
    /// <returns>The parsed instant.</returns>
    /// <exception cref="FormatException">When <paramref name="epochText"/> is not a valid integer.</exception>
    public static DateTimeOffset ParseEpoch(string epochText)
    {
        if (!long.TryParse(epochText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new FormatException("Epoch must be a valid integer.");

        var seconds = Math.Abs(value) >= MillisecondsThreshold
            ? value / 1000
            : value;

        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    /// <summary>
    /// Parses an ISO-8601 timestamp using round-trip kind rules.
    /// </summary>
    /// <param name="isoText">ISO-8601 input.</param>
    /// <returns>The parsed instant.</returns>
    /// <exception cref="FormatException">When <paramref name="isoText"/> is not a valid ISO-8601 value.</exception>
    public static DateTimeOffset ParseIso(string isoText)
    {
        if (!DateTimeOffset.TryParse(
                isoText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            throw new FormatException("ISO-8601 value is not valid.");
        }

        return parsed;
    }

    /// <summary>
    /// Builds the API response for a canonical instant.
    /// </summary>
    public static TimestampConverterResponse ToResponse(DateTimeOffset instant, TimeZoneInfo localTimeZone)
    {
        var utc = instant.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTime(instant, localTimeZone);

        return new TimestampConverterResponse(
            EpochSeconds: instant.ToUnixTimeSeconds(),
            EpochMilliseconds: instant.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            UtcDisplay: utc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            LocalDisplay: local.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
    }
}
