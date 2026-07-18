using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToCancelledCommand(WorkRequest WorkRequest, Employee CurrentUser) : StateCommandBase(WorkRequest,
    CurrentUser)
{
    public const string Name = "Cancel";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Cancelled";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.Assigned;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.Cancelled;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Creator;
    }

    public override void Execute(StateCommandContext context)
    {
        WorkRequest.AssignedDate = null;
        WorkRequest.Assignee = null;
        base.Execute(context);
    }
}