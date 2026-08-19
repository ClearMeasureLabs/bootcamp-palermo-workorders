using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses epoch or ISO-8601 inputs and builds timestamp-converter API payloads.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Converts mutually exclusive <paramref name="epochRaw"/> or <paramref name="isoRaw"/> query values.
    /// </summary>
    public static TimestampConverterOutcome Convert(string? epochRaw, string? isoRaw, TimeProvider timeProvider)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epochRaw);
        var hasIso = !string.IsNullOrWhiteSpace(isoRaw);

        if (hasEpoch && hasIso)
            return TimestampConverterOutcome.Failure("Provide either epoch or iso, not both.");

        if (!hasEpoch && !hasIso)
            return TimestampConverterOutcome.Failure("Provide either epoch or iso query parameter.");

        DateTimeOffset instant;
        if (hasEpoch)
        {
            if (!long.TryParse(epochRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                return TimestampConverterOutcome.Failure("Invalid epoch value.");

            if (!TryFromUnixTimeSeconds(seconds, out instant))
                return TimestampConverterOutcome.Failure("Epoch value is out of range.");
        }
        else
        {
            if (!DateTimeOffset.TryParse(
                    isoRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out instant))
                return TimestampConverterOutcome.Failure("Invalid ISO-8601 timestamp.");
        }

        var utcInstant = instant.ToUniversalTime();
        var now = timeProvider.GetUtcNow();
        var payload = new TimestampConverterResponse(
            EpochSeconds: utcInstant.ToUnixTimeSeconds(),
            Iso8601Utc: utcInstant.ToString("O", CultureInfo.InvariantCulture),
            Utc: FormatUtc(utcInstant),
            Local: FormatLocal(utcInstant),
            Relative: FormatRelative(utcInstant, now));

        return TimestampConverterOutcome.Success(payload);
    }

    internal static bool TryFromUnixTimeSeconds(long seconds, out DateTimeOffset instant)
    {
        try
        {
            instant = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            instant = default;
            return false;
        }
    }

    internal static string FormatUtc(DateTimeOffset utcInstant) =>
        utcInstant.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

    internal static string FormatLocal(DateTimeOffset instant)
    {
        var local = instant.ToLocalTime();
        var offset = local.Offset;
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var abs = offset.Duration();
        var offsetLabel = $"{sign}{abs.Hours:D2}:{abs.Minutes:D2}";
        return local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
               + $" ({TimeZoneInfo.Local.Id} {offsetLabel})";
    }

    internal static string FormatRelative(DateTimeOffset instant, DateTimeOffset now)
    {
        var delta = now - instant.ToUniversalTime();
        if (delta == TimeSpan.Zero)
            return "now";

        if (delta > TimeSpan.Zero)
            return FormatRelativeMagnitude(delta, future: false);

        return FormatRelativeMagnitude(-delta, future: true);
    }

    private static string FormatRelativeMagnitude(TimeSpan magnitude, bool future)
    {
        var prefix = future ? "in " : string.Empty;
        var suffix = future ? string.Empty : " ago";

        if (magnitude < TimeSpan.FromMinutes(1))
        {
            var seconds = Math.Max(1, (int)magnitude.TotalSeconds);
            return $"{prefix}{seconds} second{(seconds == 1 ? "" : "s")}{suffix}";
        }

        if (magnitude < TimeSpan.FromHours(1))
        {
            var minutes = Math.Max(1, (int)magnitude.TotalMinutes);
            return $"{prefix}{minutes} minute{(minutes == 1 ? "" : "s")}{suffix}";
        }

        if (magnitude < TimeSpan.FromDays(1))
        {
            var hours = Math.Max(1, (int)magnitude.TotalHours);
            return $"{prefix}{hours} hour{(hours == 1 ? "" : "s")}{suffix}";
        }

        if (magnitude < TimeSpan.FromDays(30))
        {
            var days = Math.Max(1, (int)magnitude.TotalDays);
            return $"{prefix}{days} day{(days == 1 ? "" : "s")}{suffix}";
        }

        if (magnitude < TimeSpan.FromDays(365))
        {
            var months = Math.Max(1, (int)(magnitude.TotalDays / 30));
            return $"{prefix}{months} month{(months == 1 ? "" : "s")}{suffix}";
        }

        var years = Math.Max(1, (int)(magnitude.TotalDays / 365));
        return $"{prefix}{years} year{(years == 1 ? "" : "s")}{suffix}";
    }
}

/// <summary>
/// Result of a timestamp conversion attempt.
/// </summary>
public sealed class TimestampConverterOutcome
{
    private TimestampConverterOutcome(TimestampConverterResponse? payload, string? error)
    {
        Payload = payload;
        Error = error;
    }

    /// <summary>Gets the converted payload when successful.</summary>
    public TimestampConverterResponse? Payload { get; }

    /// <summary>Gets the error message when conversion failed.</summary>
    public string? Error { get; }

    /// <summary>Gets whether conversion succeeded.</summary>
    public bool Succeeded => Payload is not null;

    /// <summary>Creates a successful outcome.</summary>
    public static TimestampConverterOutcome Success(TimestampConverterResponse payload) =>
        new(payload, null);

    /// <summary>Creates a failed outcome.</summary>
    public static TimestampConverterOutcome Failure(string message) =>
        new(null, message);
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and the versioned route.
/// </summary>
public record TimestampConverterResponse(
    long EpochSeconds,
    string Iso8601Utc,
    string Utc,
    string Local,
    string Relative);
