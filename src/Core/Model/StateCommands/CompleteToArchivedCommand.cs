using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;
public record CompleteToArchivedCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Archive";

    public override string TransitionVerbPresentTense => "Archive";

    public override string TransitionVerbPastTense => "Archived";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Complete;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Archived;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return (currentUser == WorkOrder.Assignee) || (currentUser == WorkOrder.Creator);
    }
}
