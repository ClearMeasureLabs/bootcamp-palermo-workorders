namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Represents an audit entry for tracking work order changes.
/// Records status changes, saves, and edits.
/// </summary>
public class AuditEntry : EntityBase<AuditEntry>
{
    public override Guid Id { get; set; }

    /// <summary>
    /// The work order this audit entry belongs to.
    /// </summary>
    public WorkOrder WorkOrder { get; set; } = null!;

    /// <summary>
    /// The employee who performed the action.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Archived name of the employee for historical tracking.
    /// </summary>
    public string? ArchivedEmployeeName { get; set; }

    /// <summary>
    /// The date and time the action occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The status before the action (null for non-status-change actions).
    /// </summary>
    public WorkOrderStatus? BeginStatus { get; set; }

    /// <summary>
    /// The status after the action (null for non-status-change actions).
    /// </summary>
    public WorkOrderStatus? EndStatus { get; set; }

    /// <summary>
    /// The action that was performed (e.g., "Save", "Assign", "Begin", "Complete", "Cancel", "Shelve").
    /// </summary>
    public string Action { get; set; } = null!;

    public override string ToString()
    {
        return $"AuditEntry: {Action} by {ArchivedEmployeeName} on {Date:G}";
    }
}
