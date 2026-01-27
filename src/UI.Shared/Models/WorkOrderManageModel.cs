using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderManageModel
{
    public const string TitleValidationPattern = "^[a-zA-Z]+$";
    public const string TitleValidationErrorMessage = "Title must contain only letters (A-Z, a-z)";
    
    public WorkOrder? WorkOrder { get; set; }
    public EditMode Mode { get; set; }

    public string? WorkOrderNumber { get; set; }

    public string? Status { get; set; }

    public string? CreatorFullName { get; set; }

    public string? AssignedToUserName { get; set; }

    [Required]
    [RegularExpression(TitleValidationPattern, ErrorMessage = TitleValidationErrorMessage)]
    public string? Title { get; set; }

    [Required] public string? Description { get; set; }

    public bool IsReadOnly { get; set; }

    public string? AssignedDate { get; set; }

    public string? CompletedDate { get; set; }

    public string? CreatedDate { get; set; }

    public string? RoomNumber { get; set; }
}