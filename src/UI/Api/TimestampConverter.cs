using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Converts between Unix epoch values and ISO-8601 timestamps.
/// </summary>
public static class TimestampConverter
{
    private const long MillisecondMagnitudeThreshold = 1_000_000_000_000L;
    private const int MaxIsoInputLength = 256;

    /// <summary>
    /// Attempts to convert a Unix epoch string (seconds or milliseconds, auto-detected by magnitude).
    /// </summary>
    public static TimestampConversionResult TryConvertFromEpoch(string? epoch)
    {
        if (string.IsNullOrWhiteSpace(epoch))
        {
            return TimestampConversionResult.Fail(
                "Epoch value is required. Provide a signed integer in seconds or milliseconds.");
        }

        var trimmed = epoch.Trim();
        if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawValue))
        {
            return TimestampConversionResult.Fail(
                "Epoch must be a signed integer (seconds or milliseconds). Decimal values are not accepted.");
        }

        var isMilliseconds = Math.Abs(rawValue) >= MillisecondMagnitudeThreshold;
        DateTimeOffset dto;
        try
        {
            dto = isMilliseconds
                ? DateTimeOffset.FromUnixTimeMilliseconds(rawValue)
                : DateTimeOffset.FromUnixTimeSeconds(rawValue);
        }
        catch (ArgumentOutOfRangeException)
        {
            return TimestampConversionResult.Fail(
                "Epoch value is out of the representable date range.");
        }

        return TimestampConversionResult.Ok(BuildResponse(dto, "epoch"));
    }

    /// <summary>
    /// Attempts to convert an ISO-8601 string to epoch and human-readable representations.
    /// </summary>
    public static TimestampConversionResult TryConvertFromIso(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return TimestampConversionResult.Fail(
                "ISO-8601 value is required. Examples: 2026-07-12T15:00:00Z, 2026-07-12T15:00:00+00:00.");
        }

        var trimmed = iso.Trim();
        if (trimmed.Length > MaxIsoInputLength)
        {
            return TimestampConversionResult.Fail(
                $"ISO-8601 input must not exceed {MaxIsoInputLength} characters.");
        }

        if (!DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
                out var dto))
        {
            return TimestampConversionResult.Fail(
                "ISO-8601 value could not be parsed. Accepted examples: 2026-07-12T15:00:00Z, 2026-07-12T15:00:00+00:00.");
        }

        return TimestampConversionResult.Ok(BuildResponse(dto, "iso"));
    }

    private static TimestampConverterResponse BuildResponse(DateTimeOffset dto, string inputKind)
    {
        var utc = dto.ToUniversalTime();
        return new TimestampConverterResponse(
            InputKind: inputKind,
            EpochSeconds: utc.ToUnixTimeSeconds(),
            EpochMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.ToString("O", CultureInfo.InvariantCulture),
            Utc: utc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            Local: dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss K", CultureInfo.InvariantCulture),
            LocalTimeZoneId: TimeZoneInfo.Local.Id);
    }
}
