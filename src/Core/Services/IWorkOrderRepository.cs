using ClearMeasure.Bootcamp.Core.Model;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.Core.Services
{
	public interface IWorkOrderRepository
	{
		Task SaveAsync(WorkOrder workOrder);
		Task<WorkOrder?> GetWorkOrderAsync(string number);
	}
}