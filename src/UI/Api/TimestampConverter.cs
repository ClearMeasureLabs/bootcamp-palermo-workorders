using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses epoch and ISO-8601 inputs and builds timestamp conversion payloads.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Parses a Unix epoch value in seconds.
    /// </summary>
    public static bool TryParseEpoch(string epochText, out DateTimeOffset dateTimeOffset, out string? error)
    {
        dateTimeOffset = default;
        error = null;

        if (!long.TryParse(epochText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            error = "epoch must be a valid Unix timestamp in seconds.";
            return false;
        }

        try
        {
            dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "epoch is outside the supported DateTimeOffset range.";
            return false;
        }
    }

    /// <summary>
    /// Parses an ISO-8601 timestamp string.
    /// </summary>
    public static bool TryParseIso(string isoText, out DateTimeOffset dateTimeOffset, out string? error)
    {
        dateTimeOffset = default;
        error = null;

        if (!DateTimeOffset.TryParse(
                isoText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out dateTimeOffset))
        {
            error = "iso must be a valid ISO-8601 timestamp.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds the API response for a UTC instant.
    /// </summary>
    public static TimestampConverterResponse ToResponse(DateTimeOffset dateTimeOffset)
    {
        var utc = dateTimeOffset.ToUniversalTime();
        return new TimestampConverterResponse(
            UnixSeconds: utc.ToUnixTimeSeconds(),
            UnixMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.ToString("O", CultureInfo.InvariantCulture),
            UtcDisplay: utc.UtcDateTime.ToString("dddd, dd MMMM yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            Rfc1123: utc.ToString("R", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
public record TimestampConverterResponse(
    long UnixSeconds,
    long UnixMilliseconds,
    string Iso8601Utc,
    string UtcDisplay,
    string Rfc1123);
