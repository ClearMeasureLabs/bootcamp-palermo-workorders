namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Represents a discrete subtask within a work order.
/// </summary>
public class WorkOrderSubtask
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>Gets or sets the parent work order identifier.</summary>
    public Guid WorkOrderId { get; set; }

    /// <summary>Gets or sets the subtask title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this subtask has been completed.</summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>Gets or sets the display order for this subtask.</summary>
    public int SortOrder { get; set; }

    /// <summary>Validates that a subtask title is non-empty and within length constraints.</summary>
    /// <exception cref="ArgumentException">Thrown when title is empty/whitespace or exceeds 300 characters.</exception>
    public static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Subtask title cannot be empty.", nameof(title));
        if (title.Length > 300)
            throw new ArgumentException("Subtask title cannot exceed 300 characters.", nameof(title));
    }
}
