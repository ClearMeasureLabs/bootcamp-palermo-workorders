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

    [Description("Lists work orders, optionally filtered by a status key. Valid status keys: DFT (Draft), ASD (Assigned), IPG (InProgress), CMP (Complete), CNL (Cancelled). Returns an error message if the status key is invalid.")]
    public async Task<object> ListWorkOrders(string? status = null)
    {
        if (status != null)
        {
            try
            {
                WorkOrderStatus.FromKey(status);
            }
            catch (ArgumentOutOfRangeException)
            {
                return $"Invalid status '{status}'. Valid status keys are: {_validStatusKeys}";
            }
        }

        var query = new WorkOrderSpecificationQuery { StatusKey = status };
        return await bus.Send(query);
    }

    private static readonly string _validStatusKeys =
        string.Join(", ", WorkOrderStatus.GetAllItems().Select(s => $"{s.Key} ({s.FriendlyName})"));

    [Description("Gets all roles defined in the system from the database, including role name and permissions.")]
    public async Task<Role[]> GetRoles()
    {
        return await bus.Send(new RoleGetAllQuery());
    }
}