namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record StateCommandResult(
    WorkOrder WorkOrder,
    string TransitionVerbPresentTense = "Save",
    string DebugMessage = "")
{
}