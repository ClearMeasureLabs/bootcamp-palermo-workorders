namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Represents an audit entry for tracking work order changes.
/// Records status transitions and save/edit operations.
/// </summary>
public class WorkOrderAuditEntry : EntityBase<WorkOrderAuditEntry>
{
    public override Guid Id { get; set; }

    /// <summary>
    /// The work order this audit entry belongs to.
    /// </summary>
    public WorkOrder WorkOrder { get; set; } = null!;

    /// <summary>
    /// The employee who performed the action.
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Archived name of the employee at the time of the action.
    /// </summary>
    public string ArchivedEmployeeName { get; set; } = "";

    /// <summary>
    /// The date and time when the action occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The status before the transition (null for non-status changes).
    /// </summary>
    public WorkOrderStatus? BeginStatus { get; set; }

    /// <summary>
    /// The status after the transition (null for non-status changes).
    /// </summary>
    public WorkOrderStatus? EndStatus { get; set; }

    /// <summary>
    /// The type of action performed (e.g., "StatusChange", "Save", "Edit").
    /// </summary>
    public string ActionType { get; set; } = "";

    /// <summary>
    /// Additional details about the action (e.g., command name).
    /// </summary>
    public string ActionDetails { get; set; } = "";
}
