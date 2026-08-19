using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses and normalizes epoch or ISO-8601 timestamp inputs for the converter API.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Values with absolute magnitude at or above this threshold are treated as Unix milliseconds; otherwise seconds.
    /// </summary>
    public const long MillisecondsThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Outcome of parsing exactly one of <paramref name="epoch"/> or <paramref name="iso"/>.
    /// </summary>
    public readonly record struct ParseResult(bool Success, DateTimeOffset Instant, string? Error);

    /// <summary>
    /// Requires exactly one of <paramref name="epoch"/> (Unix seconds or milliseconds) or <paramref name="iso"/> (ISO-8601).
    /// Values with absolute magnitude ≥ 1_000_000_000_000 are treated as milliseconds; otherwise seconds.
    /// </summary>
    public static ParseResult TryParse(string? epoch, string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (!hasEpoch && !hasIso)
        {
            return new ParseResult(false, default, "Provide exactly one of 'epoch' or 'iso' query parameters.");
        }

        if (hasEpoch && hasIso)
        {
            return new ParseResult(false, default, "Provide only one of 'epoch' or 'iso', not both.");
        }

        if (hasEpoch)
        {
            if (!long.TryParse(epoch, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochValue))
            {
                return new ParseResult(false, default, $"Unable to parse epoch value: '{epoch}'.");
            }

            var instant = Math.Abs(epochValue) >= MillisecondsThreshold
                ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
                : DateTimeOffset.FromUnixTimeSeconds(epochValue);
            return new ParseResult(true, instant.ToUniversalTime(), null);
        }

        if (!DateTimeOffset.TryParse(
                iso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedIso))
        {
            return new ParseResult(false, default, $"Unable to parse ISO-8601 value: '{iso}'.");
        }

        return new ParseResult(true, parsedIso.ToUniversalTime(), null);
    }

    /// <summary>
    /// Builds the API response for a resolved instant.
    /// </summary>
    public static TimestampConverterResponse ToResponse(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Local);
        return new TimestampConverterResponse(
            EpochSeconds: utc.ToUnixTimeSeconds(),
            EpochMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601: utc.ToString("O", CultureInfo.InvariantCulture),
            UtcFormatted: utc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            LocalFormatted: local.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
    }
}
