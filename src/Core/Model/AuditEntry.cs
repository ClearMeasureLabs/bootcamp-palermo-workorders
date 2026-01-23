namespace ClearMeasure.Bootcamp.Core.Model;

public class AuditEntry : EntityBase<AuditEntry>
{
	public override Guid Id { get; set; }
	public Guid WorkOrderId { get; set; }
	public WorkOrder? WorkOrder { get; set; }
	public string? UserName { get; set; }
	public DateTime Timestamp { get; set; }
	public string? Action { get; set; }
	public string? OldStatus { get; set; }
	public string? NewStatus { get; set; }
	public string? Details { get; set; }
}
