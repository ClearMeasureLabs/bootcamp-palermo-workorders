using System.ComponentModel.DataAnnotations;

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>Represents a reusable template for creating work orders.</summary>
public class WorkOrderTemplate : EntityBase<WorkOrderTemplate>
{
    public override Guid Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(300)]
    public string Title { get; set; } = "";

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? RoomNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid CreatedById { get; set; }

    public DateTime CreatedDate { get; set; }
}
