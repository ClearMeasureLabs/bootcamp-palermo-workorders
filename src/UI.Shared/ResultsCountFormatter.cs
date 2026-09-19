namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Formats the number of work orders found for display in the search results header.
/// </summary>
public static class ResultsCountFormatter
{
    /// <summary>
    /// Returns a localized count string, e.g. "3 work orders found".
    /// </summary>
    public static string Format(int n) => $"{n} work orders found";
}
