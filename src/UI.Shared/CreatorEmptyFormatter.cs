namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Returns an em dash (U+2014) when the full name is null, empty, or whitespace;
/// otherwise returns the full name unchanged.
/// </summary>
public static class CreatorEmptyFormatter
{
    /// <summary>
    /// Returns the em dash character for null/empty/whitespace input, else the full name unchanged.
    /// </summary>
    public static string Format(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "\u2014";

        return fullName;
    }
}
