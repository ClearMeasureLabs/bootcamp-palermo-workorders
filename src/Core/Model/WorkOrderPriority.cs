namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Defines priority levels for work orders to help staff triage and schedule maintenance tasks.
/// </summary>
public enum WorkOrderPriority
{
    /// <summary>
    /// Urgent priority - Safety hazards, critical system failures requiring immediate attention.
    /// Response time: Same day
    /// </summary>
    Urgent = 0,

    /// <summary>
    /// High priority - Important issues affecting operations but not immediately critical.
    /// Response time: Within 2-3 days
    /// </summary>
    High = 1,

    /// <summary>
    /// Normal priority - Standard maintenance and routine repairs (default).
    /// Response time: Within 1-2 weeks
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Low priority - Nice-to-have improvements and non-urgent tasks.
    /// Response time: As time permits
    /// </summary>
    Low = 3
}
