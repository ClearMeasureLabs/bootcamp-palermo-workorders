namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and the versioned route.
/// </summary>
public sealed record TimestampConverterResponse(
    /// <summary>Indicates which query parameter was supplied: <c>epoch</c> or <c>iso</c>.</summary>
    string InputKind,
    /// <summary>Unix epoch seconds for the resolved instant.</summary>
    long EpochSeconds,
    /// <summary>Unix epoch milliseconds for the resolved instant.</summary>
    long EpochMilliseconds,
    /// <summary>ISO-8601 round-trip representation of the resolved instant.</summary>
    string Iso8601,
    /// <summary>Human-readable UTC display string.</summary>
    string UtcDisplay,
    /// <summary>Human-readable display string in the server local time zone.</summary>
    string LocalDisplay);
