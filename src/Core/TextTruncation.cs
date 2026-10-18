namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Truncates display text with an ellipsis when it exceeds a maximum length.
/// </summary>
public static class TextTruncation
{
    /// <summary>
    /// Returns <paramref name="text"/> unchanged when it fits; otherwise the first
    /// <c>maxLength - 3</c> characters plus <c>...</c>. Null becomes an empty string.
    /// </summary>
    public static string WithEllipsis(string? text, int maxLength)
    {
        if (text is null)
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
