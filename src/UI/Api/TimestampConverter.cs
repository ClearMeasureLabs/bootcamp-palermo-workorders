using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses and formats timestamp inputs for <see cref="Controllers.TimestampConverterController"/>.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Absolute epoch values at or above this threshold are interpreted as milliseconds; otherwise seconds.
    /// </summary>
    public const long MillisecondThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Parses exactly one of <paramref name="epoch"/> or <paramref name="iso"/> into a UTC instant.
    /// </summary>
    public static (bool Success, DateTimeOffset Instant, string? Error) TryParse(string? epoch, string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (!hasEpoch && !hasIso)
        {
            return (false, default, "Provide exactly one of 'epoch' or 'iso' query parameters.");
        }

        if (hasEpoch && hasIso)
        {
            return (false, default, "Provide only one of 'epoch' or 'iso', not both.");
        }

        if (hasEpoch)
        {
            return TryParseEpoch(epoch!);
        }

        return TryParseIso(iso!);
    }

    /// <summary>
    /// Builds the API response for a parsed instant.
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
            LocalFormatted: FormatLocal(local));
    }

    private static string FormatLocal(DateTimeOffset local)
    {
        var formatted = local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.Local.GetUtcOffset(local.DateTime);
        var sign = zone >= TimeSpan.Zero ? "+" : "-";
        var abs = zone.Duration();
        return $"{formatted} (UTC{sign}{abs:hh\\:mm})";
    }

    private static (bool Success, DateTimeOffset Instant, string? Error) TryParseEpoch(string epochText)
    {
        if (!long.TryParse(epochText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochValue))
        {
            return (false, default, $"Epoch value '{epochText}' is not a valid integer.");
        }

        long epochMilliseconds;
        if (Math.Abs(epochValue) >= MillisecondThreshold)
        {
            epochMilliseconds = epochValue;
        }
        else
        {
            epochMilliseconds = epochValue * 1000L;
        }

        try
        {
            return (true, DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds), null);
        }
        catch (ArgumentOutOfRangeException)
        {
            return (false, default, $"Epoch value '{epochText}' is out of range.");
        }
    }

    private static (bool Success, DateTimeOffset Instant, string? Error) TryParseIso(string isoText)
    {
        if (!DateTimeOffset.TryParse(
                isoText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var instant))
        {
            return (false, default, $"ISO-8601 value '{isoText}' is not valid.");
        }

        return (true, instant, null);
    }
}
