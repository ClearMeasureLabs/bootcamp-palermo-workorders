using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderManageModel
{
    public WorkOrder? WorkOrder { get; set; }
    public EditMode Mode { get; set; }

    public string? WorkOrderNumber { get; set; }

    public string? Status { get; set; }

    public string? CreatorFullName { get; set; }

    public string? AssignedToUserName { get; set; }

    [Required] public string? Title { get; set; }

    [Required] public string? Description { get; set; }

    public bool IsReadOnly { get; set; }

    public string? AssignedDate { get; set; }

    public string? CompletedDate { get; set; }

    public string? CreatedDate { get; set; }

    public string? RoomNumber { get; set; }

    public string? OffsiteInstructions { get; set; }
}