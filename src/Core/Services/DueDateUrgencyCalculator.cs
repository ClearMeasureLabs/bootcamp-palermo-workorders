using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Computes due-date urgency at read time using America/Chicago calendar days.
/// </summary>
public static class DueDateUrgencyCalculator
{
    /// <summary>
    /// Returns urgency for the given work order relative to Chicago "today".
    /// </summary>
    public static DueDateUrgency Calculate(WorkOrder workOrder, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workOrder);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (workOrder.DueDate is null)
        {
            return DueDateUrgency.None;
        }

        if (!IsOpen(workOrder.Status))
        {
            return DueDateUrgency.None;
        }

        var today = ChurchTimeZone.Today(timeProvider);
        if (workOrder.DueDate.Value == today)
        {
            return DueDateUrgency.DueToday;
        }

        if (workOrder.DueDate.Value < today)
        {
            return DueDateUrgency.Overdue;
        }

        return DueDateUrgency.None;
    }

    /// <summary>
    /// CSS class for the Due Date cell only (empty when no urgency color).
    /// </summary>
    public static string CssClass(DueDateUrgency urgency) =>
        urgency switch
        {
            DueDateUrgency.DueToday => "due-date-today",
            DueDateUrgency.Overdue => "due-date-overdue",
            _ => string.Empty
        };

    /// <summary>
    /// Accessible label next to the date when urgency applies.
    /// </summary>
    public static string? ScreenReaderText(DueDateUrgency urgency) =>
        urgency switch
        {
            DueDateUrgency.DueToday => "Due today",
            DueDateUrgency.Overdue => "Overdue",
            _ => null
        };

    private static bool IsOpen(WorkOrderStatus status) =>
        status == WorkOrderStatus.Draft
        || status == WorkOrderStatus.Assigned
        || status == WorkOrderStatus.InProgress;
}
