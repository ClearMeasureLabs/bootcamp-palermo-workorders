namespace ClearMeasure.Bootcamp.Core.Model;

public class AuditEntry
{
    public Guid WorkOrderId { get; set; }
    
    public int Sequence { get; set; }
    
    public Guid? EmployeeId { get; set; }
    
    public string? ArchivedEmployeeName { get; set; }
    
    public DateTime Date { get; set; }
    
    public WorkOrderStatus? BeginStatus { get; set; }
    
    public WorkOrderStatus? EndStatus { get; set; }
}
