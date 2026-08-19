using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses epoch or ISO-8601 inputs and builds timestamp converter responses.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Values with absolute magnitude at or above this threshold are treated as Unix milliseconds; otherwise seconds.
    /// </summary>
    public const long MillisecondsThreshold = 1_000_000_000_000L;

    private const string UtcFormat = "yyyy-MM-dd HH:mm:ss 'UTC'";
    private const string LocalFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Validates input and parses exactly one of <paramref name="epoch"/> or <paramref name="iso"/> into a UTC instant.
    /// </summary>
    public static (bool Success, DateTimeOffset Instant, string? Error) TryParse(long? epoch, string? iso)
    {
        var hasEpoch = epoch.HasValue;
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (!hasEpoch && !hasIso)
        {
            return (false, default, "Exactly one of 'epoch' or 'iso' query parameter is required.");
        }

        if (hasEpoch && hasIso)
        {
            return (false, default, "Supply only one of 'epoch' or 'iso', not both.");
        }

        if (hasEpoch)
        {
            var value = epoch!.Value;
            var instant = Math.Abs(value) >= MillisecondsThreshold
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return (true, instant.ToUniversalTime(), null);
        }

        if (!DateTimeOffset.TryParse(
                iso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return (false, default, $"Unable to parse ISO-8601 value '{iso}'.");
        }

        return (true, parsed.ToUniversalTime(), null);
    }

    /// <summary>
    /// Builds the API response for a parsed UTC instant.
    /// </summary>
    public static TimestampConverterResponse BuildResponse(DateTimeOffset instantUtc)
    {
        var utc = instantUtc.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Local);
        return new TimestampConverterResponse(
            EpochSeconds: utc.ToUnixTimeSeconds(),
            EpochMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601: utc.ToString("O", CultureInfo.InvariantCulture),
            UtcFormatted: utc.ToString(UtcFormat, CultureInfo.InvariantCulture),
            LocalFormatted: local.ToString(LocalFormat, CultureInfo.InvariantCulture));
    }
}
