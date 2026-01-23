namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Exception thrown when work order validation fails.
/// </summary>
public class WorkOrderValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkOrderValidationException"/> class.
    /// </summary>
    /// <param name="validationErrors">The list of validation error messages.</param>
    public WorkOrderValidationException(IEnumerable<string> validationErrors)
        : base(BuildMessage(validationErrors))
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkOrderValidationException"/> class.
    /// </summary>
    /// <param name="validationError">A single validation error message.</param>
    public WorkOrderValidationException(string validationError)
        : this(new[] { validationError })
    {
    }

    private static string BuildMessage(IEnumerable<string> validationErrors)
    {
        return "Work order validation failed: " + string.Join("; ", validationErrors);
    }
}
