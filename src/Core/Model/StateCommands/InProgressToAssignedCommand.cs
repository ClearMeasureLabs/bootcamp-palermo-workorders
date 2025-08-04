using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;
public record InProgressToAssignedCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public static string Name { get; set; } = "Shelve";

    public override string TransitionVerbPresentTense => "Shelve";

    public override string TransitionVerbPastTense => "Shelved";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }
}
