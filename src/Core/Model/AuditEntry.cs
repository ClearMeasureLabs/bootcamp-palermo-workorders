namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Records an audit entry for every status change and every save or edit on a work order.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    public WorkOrder? WorkOrder { get; set; }

    public DateTime Date { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string? EmployeeName { get; set; }

    public WorkOrderStatus? BeginStatus { get; set; }

    public WorkOrderStatus? EndStatus { get; set; }

    public string? Action { get; set; }
}
