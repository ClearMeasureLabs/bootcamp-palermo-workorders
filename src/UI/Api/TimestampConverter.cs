using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses epoch and ISO-8601 timestamp inputs and builds API responses.
/// </summary>
public static class TimestampConverter
{
    private const long SecondsUpperBound = 9_999_999_999L;

    /// <summary>
    /// Attempts to parse a Unix epoch value (seconds or milliseconds) into a <see cref="DateTimeOffset"/>.
    /// </summary>
    public static bool TryFromEpoch(string? epoch, out DateTimeOffset instant, out string? error)
    {
        instant = default;
        error = null;

        if (string.IsNullOrWhiteSpace(epoch))
        {
            error = "Epoch value is required.";
            return false;
        }

        if (!long.TryParse(epoch, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            error = "Epoch must be a numeric Unix timestamp.";
            return false;
        }

        try
        {
            instant = Math.Abs(value) > SecondsUpperBound
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "Epoch value is outside the representable DateTimeOffset range.";
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse an ISO-8601 string into a <see cref="DateTimeOffset"/>.
    /// </summary>
    public static bool TryFromIso8601(string? iso, out DateTimeOffset instant, out string? error)
    {
        instant = default;
        error = null;

        if (string.IsNullOrWhiteSpace(iso))
        {
            error = "ISO-8601 value is required.";
            return false;
        }

        if (DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out instant))
        {
            return true;
        }

        error = "ISO-8601 value could not be parsed.";
        return false;
    }

    /// <summary>
    /// Builds a <see cref="TimestampConverterResponse"/> from a resolved instant.
    /// </summary>
    public static TimestampConverterResponse ToResponse(DateTimeOffset instant, string inputKind)
    {
        var utc = instant.ToUniversalTime();
        var local = instant.ToLocalTime();

        return new TimestampConverterResponse(
            InputKind: inputKind,
            EpochSeconds: instant.ToUnixTimeSeconds(),
            EpochMilliseconds: instant.ToUnixTimeMilliseconds(),
            Iso8601: instant.ToString("O", CultureInfo.InvariantCulture),
            UtcDisplay: $"{utc:yyyy-MM-dd HH:mm:ss} UTC",
            LocalDisplay: local.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
    }
}
