using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record SaveDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) :
StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Save";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Draft;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Saved";

    public override void Execute(StateCommandContext context)
    {
        if (WorkOrder.CreatedDate.Equals(null))
        {
            WorkOrder.CreatedDate = context.CurrentDateTime;
        }

        if (WorkOrder.Description == null)
        {
            WorkOrder.Description = string.Empty;
        }
        else if (WorkOrder.Description.Length > 0)
        {
            WorkOrder.Description = WorkOrder.Description.ToUpperInvariant();
        }

        base.Execute(context);
    }
}