namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
/// <param name="EpochSeconds">Unix epoch in whole seconds (UTC).</param>
/// <param name="EpochMilliseconds">Unix epoch in milliseconds (UTC).</param>
/// <param name="Iso8601">ISO-8601 round-trip (<c>O</c>) representation in UTC.</param>
/// <param name="UtcFormatted">Human-readable UTC timestamp (invariant culture).</param>
/// <param name="LocalFormatted">Human-readable local timestamp using server <see cref="TimeZoneInfo.Local"/> (invariant culture).</param>
public sealed record TimestampConverterResponse(
    long EpochSeconds,
    long EpochMilliseconds,
    string Iso8601,
    string UtcFormatted,
    string LocalFormatted);
