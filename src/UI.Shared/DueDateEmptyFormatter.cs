namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Returns an em dash (U+2014) when the due-date display string is null, empty, or whitespace.
/// </summary>
public static class DueDateEmptyFormatter
{
    /// <summary>
    /// Returns the em dash character U+2014 for null/empty/whitespace inputs,
    /// else the input unchanged.
    /// </summary>
    public static string Format(string? dueDateDisplay)
    {
        if (string.IsNullOrWhiteSpace(dueDateDisplay))
            return "\u2014";

        return dueDateDisplay;
    }
}
