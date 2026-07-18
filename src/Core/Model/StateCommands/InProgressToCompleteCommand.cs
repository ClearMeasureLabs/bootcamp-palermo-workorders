using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToCompleteCommand(WorkRequest WorkRequest, Employee CurrentUser) : StateCommandBase(WorkRequest,
CurrentUser)
{
    public const string Name = "Complete";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Completed";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.InProgress;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.Complete;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Assignee;
    }

    public override void Execute(StateCommandContext context)
    {
        WorkRequest.CompletedDate = context.CurrentDateTime;
        base.Execute(context);
    }
}