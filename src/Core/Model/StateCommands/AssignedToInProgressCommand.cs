namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToInProgressCommand(WorkRequest WorkRequest, Employee CurrentUser)
: StateCommandBase(WorkRequest, CurrentUser)
{
    public const string Name = "Begin";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.Assigned;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Begun";
}