using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CompleteToInProgressCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder,
CurrentUser)
{
    public const string Name = "Reopen";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Reopened";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Complete;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.CompletedDate = null;
        base.Execute(context);
    }
}
