using ClearMeasure.Bootcamp.Core.Model.Constants;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DraftToAssignedCommand(WorkRequest WorkRequest, Employee CurrentUser)
: StateCommandBase(WorkRequest, CurrentUser)
{
    public const string Name = "Assign";

    public override WorkRequestStatus GetBeginStatus()
    {
        return WorkRequestStatus.Draft;
    }

    public override WorkRequestStatus GetEndStatus()
    {
        return WorkRequestStatus.Assigned;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Assigned";

    public override void Execute(StateCommandContext context)
    {
        WorkRequest.AssignedDate = context.CurrentDateTime;
        base.Execute(context);

        var assignedToAiBot = WorkRequest.Assignee?.Roles
            .Any(x => x.Name == Roles.Bot) ?? false;

        if (assignedToAiBot)
        {
            StateTransitionEvent = new WorkRequestAssignedToBotEvent(WorkRequest.Number ?? string.Empty, WorkRequest.Assignee!.Id);
        }
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkRequest.Creator;
    }
}