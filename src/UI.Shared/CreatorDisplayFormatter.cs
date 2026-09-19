namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for work order creator/assignee names in the search results table.
/// </summary>
public static class CreatorDisplayFormatter
{
    /// <summary>
    /// Formats a creator or assignee name as "LASTNAME, F." (uppercase).
    /// Returns string.Empty when lastName is null or empty.
    /// Returns only the uppercase last name when firstName is null or empty.
    /// </summary>
    public static string Format(string? lastName, string? firstName)
    {
        if (string.IsNullOrEmpty(lastName))
            return string.Empty;

        if (string.IsNullOrEmpty(firstName))
            return lastName.ToUpperInvariant();

        return lastName.ToUpperInvariant() + ", " + char.ToUpperInvariant(firstName[0]) + ".";
    }
}
