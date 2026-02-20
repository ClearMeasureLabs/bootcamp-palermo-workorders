using ClearMeasure.Bootcamp.Core.Model.Constants;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DraftToAssignedCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Assign";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Draft;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Assigned";

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.AssignedDate = context.CurrentDateTime;
        base.Execute(context);

        var assignedToAiBot = WorkOrder.Assignee?.Roles
            .Any(x => x.Name == Roles.Bot) ?? false;

        if (assignedToAiBot)
        {
            StateTransitionEvent = new WorkOrderAssignedToBotEvent(WorkOrder.Number, WorkOrder.Assignee!.Id);
        }
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }
}