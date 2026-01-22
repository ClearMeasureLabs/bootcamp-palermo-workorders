namespace ClearMeasure.Bootcamp.Core.Model;

public class AuditEntry
{
    public Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    public WorkOrder? WorkOrder { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string? EmployeeName { get; set; }

    public DateTime EntryDate { get; set; }

    public string? BeginStatus { get; set; }

    public string? EndStatus { get; set; }

    public string? Action { get; set; }
}
