using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services
{
	public interface IStateCommandVisitor
	{
		void SaveWorkOrder(WorkOrder workOrder);
		void EditWorkOrder(WorkOrder workOrder);
	}
}