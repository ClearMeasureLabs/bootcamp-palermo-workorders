using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record SaveDraftCommand(WorkRequest WorkRequest, Employee CurrentUser) :
StateCommandBase(WorkRequest, CurrentUser)
{
    public const string Name = "Save";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.Draft;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.Draft;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Creator;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Saved";

    public override void Execute(StateCommandContext context)
    {
        if (WorkRequest.CreatedDate.Equals(null))
        {
            WorkRequest.CreatedDate = context.CurrentDateTime;
        }

        base.Execute(context);
    }
}