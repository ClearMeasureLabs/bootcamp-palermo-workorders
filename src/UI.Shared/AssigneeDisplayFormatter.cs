namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the assignee column in work-order search tables.
/// Returns "Unassigned" when the full name is null, empty, or whitespace.
/// </summary>
public static class AssigneeDisplayFormatter
{
    /// <summary>
    /// Returns "Unassigned" for null/empty/whitespace; otherwise returns the original full name.
    /// </summary>
    public static string Format(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Unassigned";

        return fullName;
    }
}
