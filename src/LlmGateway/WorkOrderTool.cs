using System.ComponentModel;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkOrderTool(IBus bus)
{
    [Description("Retrieves a specific work order by its unique number. " +
                 "Returns the full work order including title, description, room number, status, " +
                 "the employee who created it (creator), and the employee it is assigned to (assignee). " +
                 "Use this when the user asks about a specific work order, its details, status, or who is involved.")]
    public async Task<WorkOrder?> GetWorkOrderByNumber(
        [Description("The unique work order number, e.g. 'WO-001'. This is the short identifier displayed in the UI.")] string workOrderNumber)
    {
        return await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
    }

    [Description("Retrieves the complete list of all employees in the system. " +
                 "Each employee includes their username, first name, last name, email address, and assigned roles. " +
                 "Roles indicate whether an employee can create or fulfill work orders. " +
                 "Use this when the user asks about employees, staff, team members, who can be assigned, or who is available.")]
    public async Task<Employee[]> GetAllEmployees()
    {
        return await bus.Send(new EmployeeGetAllQuery());
    }
}