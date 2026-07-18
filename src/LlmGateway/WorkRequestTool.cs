using System.ComponentModel;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkRequestTool(IBus bus)
{
    [Description("Retrieves a specific work request by its unique number. " +
                 "Returns the full work request including title, description, room number, status, " +
                 "the employee who created it (creator), and the employee it is assigned to (assignee). " +
                 "Use this when the user asks about a specific work request, its details, status, or who is involved.")]
    public async Task<WorkRequest?> GetWorkRequestByNumber(
        [Description("The unique work request number, e.g. 'WO-001'. This is the short identifier displayed in the UI.")] string workRequestNumber)
    {
        return await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));
    }

    [Description("Retrieves the complete list of all employees in the system. " +
                 "Each employee includes their username, first name, last name, email address, and assigned roles. " +
                 "Roles indicate whether an employee can create or fulfill work requests. " +
                 "Use this when the user asks about employees, staff, team members, who can be assigned, or who is available.")]
    public async Task<Employee[]> GetAllEmployees()
    {
        return await bus.Send(new EmployeeGetAllQuery());
    }
}