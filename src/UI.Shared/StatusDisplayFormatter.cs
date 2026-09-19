namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for work-order status badges.
/// </summary>
public static class StatusDisplayFormatter
{
    /// <summary>
    /// Returns the friendly name when non-null and non-whitespace, else string.Empty.
    /// </summary>
    public static string Format(string? friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            return string.Empty;

        return friendlyName;
    }
}
