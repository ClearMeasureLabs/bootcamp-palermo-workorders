namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>Represents a reusable template for creating work orders.</summary>
public class WorkOrderTemplate : EntityBase<WorkOrderTemplate>
{
    public override Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public string? RoomNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid CreatedById { get; set; }

    public DateTime CreatedDate { get; set; }
}
