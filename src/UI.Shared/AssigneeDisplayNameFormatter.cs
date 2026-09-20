using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for work order assignee names.
/// </summary>
public static class AssigneeDisplayNameFormatter
{
    /// <summary>
    /// Returns the assignee's full name, or "Unassigned" when there is no assignee.
    /// </summary>
    public static string FormatForDisplay(Employee? assignee)
    {
        return assignee?.GetFullName() ?? "Unassigned";
    }
}
