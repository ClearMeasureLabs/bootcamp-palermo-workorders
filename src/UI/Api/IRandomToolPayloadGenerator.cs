namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Produces sample random values for <c>GET /api/tools/random</c> integration and testing.
/// </summary>
public interface IRandomToolPayloadGenerator
{
    /// <summary>
    /// Returns a pseudo-random 32-bit signed integer.
    /// </summary>
    int NextInt32();

    /// <summary>
    /// Returns a pseudo-random alphanumeric string of the given length.
    /// </summary>
    string NextAlphanumericString(int length);

    /// <summary>
    /// Returns a new RFC 4122 UUID.
    /// </summary>
    Guid NextGuid();

    /// <summary>
    /// Returns a CSS hex color in the form <c>#rrggbb</c> (lowercase hex digits).
    /// </summary>
    string NextCssHexColor();
}
