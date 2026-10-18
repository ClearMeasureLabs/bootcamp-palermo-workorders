namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public enum EditMode
{
    New,
    Edit
}

/// <summary>
/// Resolves the manage-page edit mode from a query-string value.
/// </summary>
public static class EditModeResolver
{
    /// <summary>
    /// Returns <see cref="EditMode.Edit"/> when <paramref name="mode"/> equals "edit" (ordinal ignore case); otherwise <see cref="EditMode.New"/>.
    /// </summary>
    public static EditMode FromQuery(string? mode) =>
        string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase) ? EditMode.Edit : EditMode.New;
}
