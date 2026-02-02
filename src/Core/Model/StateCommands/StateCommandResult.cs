namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record StateCommandResult(
    WorkOrder WorkOrder,
    string TransitionVerbPresentTense = "Save",
    string DebugMessage = "",
    IReadOnlyList<string>? ValidationErrors = null)
{
    /// <summary>
    /// Indicates whether the command executed successfully without validation errors.
    /// </summary>
    public bool IsSuccess => ValidationErrors == null || ValidationErrors.Count == 0;

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    public static StateCommandResult Failure(WorkOrder workOrder, IReadOnlyList<string> validationErrors)
    {
        return new StateCommandResult(workOrder, ValidationErrors: validationErrors);
    }
}