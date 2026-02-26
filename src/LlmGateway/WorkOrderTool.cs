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

    [Description("Can list work orders created by a user. Pass the user's username (e.g. tlovejoy). Returns the work orders that user created.")]
    public async Task<WorkOrder[]> GetWorkOrdersByCreatorUserName(string userName)
    {
        var employee = await bus.Send(new EmployeeByUserNameQuery(userName));
        var query = new WorkOrderSpecificationQuery();
        query.MatchCreator(employee);
        return await bus.Send(query);
    }
}