namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Represents an audit entry recording a status change or edit on a work order.
/// </summary>
public class AuditEntry : EntityBase<AuditEntry>
{
    public override Guid Id { get; set; }

    /// <summary>
    /// The work order this audit entry is associated with.
    /// </summary>
    public WorkOrder? WorkOrder { get; set; }

    /// <summary>
    /// The ID of the work order (for EF Core navigation).
    /// </summary>
    public Guid WorkOrderId { get; set; }

    /// <summary>
    /// The employee who performed the action.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// The ID of the employee who performed the action.
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// Archived name of the employee at the time of the action.
    /// </summary>
    public string? ArchivedEmployeeName { get; set; }

    /// <summary>
    /// The date and time when the action occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The status before the action.
    /// </summary>
    public WorkOrderStatus? BeginStatus { get; set; }

    /// <summary>
    /// The status after the action.
    /// </summary>
    public WorkOrderStatus? EndStatus { get; set; }

    /// <summary>
    /// The action that was performed (e.g., "Save", "Assign", "Complete", "Cancel", "Shelve").
    /// </summary>
    public string? Action { get; set; }
}
