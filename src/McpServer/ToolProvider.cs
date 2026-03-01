using System.ComponentModel;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Wraps MCP <see cref="WorkOrderTools"/> and <see cref="EmployeeTools"/> as <see cref="AITool"/> instances.
/// </summary>
public class ToolProvider(IBus bus, IWorkOrderNumberGenerator numberGenerator) : IToolProvider
{
    public IList<AITool> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(ListWorkOrders),
            AIFunctionFactory.Create(GetWorkOrder),
            AIFunctionFactory.Create(CreateWorkOrder),
            AIFunctionFactory.Create(ExecuteWorkOrderCommand),
            AIFunctionFactory.Create(UpdateWorkOrderDescription),
            AIFunctionFactory.Create(ListEmployees),
            AIFunctionFactory.Create(GetEmployee),
        ];
    }

    [Description("Lists all work orders, optionally filtered by status. Valid statuses: Draft, Assigned, InProgress, Complete.")]
    private Task<string> ListWorkOrders(
        [Description("Optional status filter (Draft, Assigned, InProgress, Complete)")] string? status = null)
    {
        return WorkOrderTools.ListWorkOrders(bus, status);
    }

    [Description("Retrieves a single work order by its number, including full details.")]
    private Task<string> GetWorkOrder(
        [Description("The work order number")] string workOrderNumber)
    {
        return WorkOrderTools.GetWorkOrder(bus, workOrderNumber);
    }

    [Description("Creates a new draft work order. Requires a title, description, and the username of the creator. Optionally accepts a room number for the location.")]
    private Task<string> CreateWorkOrder(
        [Description("Title of the work order")] string title,
        [Description("Description of the work order")] string description,
        [Description("Username of the employee creating the work order")] string creatorUsername,
        [Description("Optional room number or location for the work order")] string? roomNumber = null)
    {
        return WorkOrderTools.CreateWorkOrder(bus, numberGenerator, title, description, creatorUsername, roomNumber);
    }

    [Description("Executes a state command on a work order. Available commands: DraftToAssignedCommand (requires assigneeUsername), AssignedToInProgressCommand, InProgressToCompleteCommand.")]
    private Task<string> ExecuteWorkOrderCommand(
        [Description("The work order number")] string workOrderNumber,
        [Description("The command name (e.g., DraftToAssignedCommand)")] string commandName,
        [Description("Username of the employee executing the command")] string executingUsername,
        [Description("Username of the employee to assign the work order to (required for DraftToAssignedCommand)")] string? assigneeUsername = null)
    {
        return WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, commandName, executingUsername, assigneeUsername);
    }

    [Description("Updates the description of an existing work order.")]
    private Task<string> UpdateWorkOrderDescription(
        [Description("The work order number")] string workOrderNumber,
        [Description("The new description")] string newDescription,
        [Description("Username of the employee making the update")] string updatingUsername)
    {
        return WorkOrderTools.UpdateWorkOrderDescription(bus, workOrderNumber, newDescription, updatingUsername);
    }

    [Description("Lists all employees in the system with their username, name, email, and roles.")]
    private Task<string> ListEmployees()
    {
        return EmployeeTools.ListEmployees(bus);
    }

    [Description("Retrieves a single employee by username.")]
    private Task<string> GetEmployee(
        [Description("The employee's username")] string username)
    {
        return EmployeeTools.GetEmployee(bus, username);
    }
}
