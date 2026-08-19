using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Parses unix epoch inputs (seconds or milliseconds heuristic) and ISO-8601 instants for timestamp conversion APIs.
/// </summary>
internal static class TimestampConverterParsing
{
    internal const long MillisecondsThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Interprets a unix epoch numeric value as seconds unless its absolute value meets the millisecond heuristic threshold.
    /// </summary>
    internal static DateTimeOffset ParseUnixEpochNumeric(long raw)
    {
        if (Math.Abs(raw) >= MillisecondsThreshold)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(raw);
        }

        return DateTimeOffset.FromUnixTimeSeconds(raw);
    }

    /// <summary>
    /// Parses an ISO-8601 instant using invariant culture rules suitable for APIs.
    /// </summary>
    internal static bool TryParseIso8601(string iso, out DateTimeOffset instant)
    {
        return DateTimeOffset.TryParse(
            iso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out instant);
    }
}
