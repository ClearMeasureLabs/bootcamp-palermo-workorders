using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;
public record InProgressToCancelledCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
	public override string TransitionVerbPresentTense => "Cancel";

	public override string TransitionVerbPastTense => "Cancelled";

	public override WorkOrderStatus GetBeginStatus()
	{
		throw new NotImplementedException();
	}

	public override WorkOrderStatus GetEndStatus()
	{
		throw new NotImplementedException();
	}

	protected override bool UserCanExecute(Employee currentUser)
	{
		throw new NotImplementedException();
	}
}
