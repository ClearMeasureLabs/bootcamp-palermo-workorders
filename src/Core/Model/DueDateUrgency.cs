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
    Overdue = 2,

    /// <summary>Open work order due 1–2 Chicago calendar days from today.</summary>
    DueSoon = 3
}
