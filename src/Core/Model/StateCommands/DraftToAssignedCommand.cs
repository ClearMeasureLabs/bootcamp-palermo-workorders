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

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Assigned";

    public override void Execute(StateCommandContext context)
    {
        ValidateWorkOrder();
        
        WorkOrder.AssignedDate = context.CurrentDateTime;
        base.Execute(context);
    }

    private void ValidateWorkOrder()
    {
        if (string.IsNullOrWhiteSpace(WorkOrder.Title))
        {
            throw new InvalidOperationException("Title cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(WorkOrder.Description))
        {
            throw new InvalidOperationException("Description cannot be empty");
        }
    }
}