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

    [StringLength(WorkOrder.InstructionsMaxLength, ErrorMessage = "Instructions cannot exceed 4000 characters.")]
    public string? Instructions { get; set; }

    public bool IsReadOnly { get; set; }

    public string? AssignedDate { get; set; }

    public string? CompletedDate { get; set; }

    public string? CreatedDate { get; set; }

    /// <summary>
    /// Optional due date bound to the native date picker. Null when unset.
    /// </summary>
    public DateOnly? DueDateInput { get; set; }

    /// <summary>
    /// Display text for due date (MMM d, yyyy) when set.
    /// </summary>
    public string? DueDateDisplay { get; set; }

    /// <summary>
    /// CSS class for due-date urgency on the date cell only.
    /// </summary>
    public string DueDateCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Screen-reader text for due-date urgency ("Due today" / "Overdue").
    /// </summary>
    public string? DueDateUrgencyText { get; set; }

    [StringLength(WorkOrder.RoomNumberMaxLength, ErrorMessage = "Room cannot exceed 900 characters.")]
    public string? RoomNumber { get; set; }
}