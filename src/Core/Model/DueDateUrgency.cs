namespace ClearMeasure.Bootcamp.Core.Model;

// Read-time urgency for an optional work-order due date. Never persisted.
public enum DueDateUrgency
{
    None = 0,

    DueToday = 1,

    Overdue = 2
}
