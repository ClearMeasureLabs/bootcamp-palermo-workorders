namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Read-time urgency for an optional work-order due date. Never persisted.
/// </summary>
public enum DueDateUrgency
{
    /// <summary>No due date, or work order is Complete/Cancelled.</summary>
    None = 0,

    /// <summary>Open work order due today in America/Chicago.</summary>
    DueToday = 1,

    /// <summary>Open work order with a due date before today in America/Chicago.</summary>
    Overdue = 2
}
