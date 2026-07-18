namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToAssignedCommand(WorkRequest WorkRequest, Employee CurrentUser)
: StateCommandBase(WorkRequest, CurrentUser)
{
    public const string Name = "Shelve";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.InProgress;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.Assigned;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Shelved";
}