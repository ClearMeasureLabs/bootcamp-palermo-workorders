using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

public record DeleteDraftCommand : StateCommandBase
{
    public DeleteDraftCommand(WorkOrder workOrder, Employee currentUser)
        : base(workOrder, currentUser)
    {
    }

    public override WorkOrderStatus GetBeginStatus() => WorkOrderStatus.Draft;

    public override WorkOrderStatus GetEndStatus() => WorkOrderStatus.None; // Indicates deletion

    protected override bool UserCanExecute(Employee currentUser)
    {
        return WorkOrder.Creator != null && WorkOrder.Creator.Equals(currentUser);
    }

    public override string TransitionVerbPresentTense => "Delete";
    public override string TransitionVerbPastTense => "Deleted";

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.Status = GetEndStatus();
        base.Execute(context);
    }
}