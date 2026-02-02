namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Represents an audit entry that records changes to a work order.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    public WorkOrder? WorkOrder { get; set; }

    public int Sequence { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string? ArchivedEmployeeName { get; set; }

    public DateTime? Date { get; set; }

    public WorkOrderStatus? BeginStatus { get; set; }

    public WorkOrderStatus? EndStatus { get; set; }

    public string? ActionType { get; set; }
}
