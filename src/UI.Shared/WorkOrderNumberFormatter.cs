namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Formats a raw work-order number with the standard "WO-" prefix for display.
/// </summary>
public static class WorkOrderNumberFormatter
{
    /// <summary>
    /// Returns the work-order number with the "WO-" prefix.
    /// </summary>
    public static string Format(string? number)
    {
        if (string.IsNullOrEmpty(number))
            return string.Empty;

        return $"WO-{number}";
    }
}
