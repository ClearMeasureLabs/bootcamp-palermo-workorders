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
        return Calculate(workOrder.DueDate, workOrder.Status, timeProvider);
    }

    /// <summary>
    /// Returns urgency for an editable due date and work-order status relative to Chicago "today".
    /// </summary>
    public static DueDateUrgency Calculate(
        DateOnly? dueDate,
        WorkOrderStatus status,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (dueDate is null)
        {
            return DueDateUrgency.None;
        }

        if (!IsOpen(status))
        {
            return DueDateUrgency.None;
        }

        return ForOpenDueDate(dueDate.Value, ChurchTimeZone.Today(timeProvider));
    }

    /// <summary>
    /// CSS class for the Due Date cell only (empty when no urgency color).
    /// </summary>
    public static string CssClass(DueDateUrgency urgency) =>
        urgency switch
        {
            DueDateUrgency.DueToday => "due-date-today",
            DueDateUrgency.Overdue => "due-date-overdue",
            DueDateUrgency.DueSoon => "due-date-soon",
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
            DueDateUrgency.DueSoon => "Due soon",
            _ => null
        };

    /// <summary>
    /// Badge label for the urgency pill shown in the work order list.
    /// Returns null when no badge should be rendered (no due date).
    /// </summary>
    public static string? BadgeText(DueDateUrgency urgency, DateOnly? dueDate) =>
        urgency switch
        {
            DueDateUrgency.Overdue => "Overdue",
            DueDateUrgency.DueToday => "Due Today",
            _ => dueDate.HasValue ? "On Track" : null
        };

    private static DueDateUrgency ForOpenDueDate(DateOnly dueDate, DateOnly today)
    {
        var daysUntilDue = dueDate.DayNumber - today.DayNumber;
        if (daysUntilDue < 0)
        {
            return DueDateUrgency.Overdue;
        }

        if (daysUntilDue == 0)
        {
            return DueDateUrgency.DueToday;
        }

        if (daysUntilDue <= 2)
        {
            return DueDateUrgency.DueSoon;
        }

        return DueDateUrgency.None;
    }

    private static bool IsOpen(WorkOrderStatus status) =>
        status == WorkOrderStatus.Draft
        || status == WorkOrderStatus.Assigned
        || status == WorkOrderStatus.InProgress;
}
