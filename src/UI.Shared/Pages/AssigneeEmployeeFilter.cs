using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Filters employees for the work-order assignee dropdown.
/// </summary>
public static class AssigneeEmployeeFilter
{
    /// <summary>
    /// Returns active employees, always including the current assignee when provided.
    /// </summary>
    public static IEnumerable<Employee> Filter(
        IEnumerable<Employee> employees,
        string? currentAssigneeUserName)
    {
        return employees.Where(e => e.IsActive || e.UserName == currentAssigneeUserName);
    }
}
