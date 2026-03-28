using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CompleteToAssignedCommand(WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Reassign";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Complete;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Reassigned";

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.AssignedDate = context.CurrentDateTime;
        WorkOrder.CompletedDate = null;
        base.Execute(context);
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }
}
