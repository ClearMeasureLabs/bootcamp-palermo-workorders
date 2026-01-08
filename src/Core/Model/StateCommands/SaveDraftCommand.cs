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
        // Validate first (from base class)
        if (GetBeginStatus() == WorkOrderStatus.Draft)
        {
            ValidateWorkOrder();
        }
        
        // Set CreatedDate if not already set
        if (WorkOrder.CreatedDate == null)
        {
            WorkOrder.CreatedDate = context.CurrentDateTime;
        }

        // Change status (from base class, but inline to avoid double validation)
        var currentUserFullName = CurrentUser.GetFullName();
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
    }
}