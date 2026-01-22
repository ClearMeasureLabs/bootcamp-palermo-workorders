namespace ClearMeasure.Bootcamp.Core.Model;

public class AuditEntry : EntityBase<AuditEntry>
{
    public Guid WorkOrderId { get; set; }
    
    public int Sequence { get; set; }
    
    public Guid? EmployeeId { get; set; }
    
    public Employee? Employee { get; set; }
    
    public string? ArchivedEmployeeName { get; set; }
    
    public DateTime? Date { get; set; }
    
    public WorkOrderStatus BeginStatus { get; set; } = WorkOrderStatus.None;
    
    public WorkOrderStatus EndStatus { get; set; } = WorkOrderStatus.None;
    
    public WorkOrder? WorkOrder { get; set; }

    public override Guid Id { get; set; }
}
