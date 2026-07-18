namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record StateCommandResult(
    WorkRequest WorkRequest,
    string TransitionVerbPresentTense = "Save",
    string DebugMessage = "")
{
}