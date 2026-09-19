namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for work order assignee names.
/// Returns "Unassigned" for null, empty, or whitespace values.
/// </summary>
public static class AssigneeDisplayFormatter
{
    /// <summary>
    /// Returns "Unassigned" for null/empty/whitespace, else the fullName unchanged.
    /// </summary>
    public static string Format(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Unassigned";

        return fullName;
    }
}
