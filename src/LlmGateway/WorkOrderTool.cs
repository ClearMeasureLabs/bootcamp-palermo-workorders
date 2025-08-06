using System.ComponentModel;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkOrderTool(IBus bus)
{
    [Description("Can get or retrieve WorkOrder given the work order number. Retrieves the employee creator and assignee, or who the work order is assigned to")]
    public async Task<WorkOrder?> GetWorkOrderByNumber(string workOrderNumber)
    {
        return await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
    }

    [Description("Can get or retrieve Employees Retrieves all the employees in the system along with their roles and names. ")]
    public async Task<Employee[]> GetAllEmployees()
    {
        return await bus.Send(new EmployeeGetAllQuery());
    }
}