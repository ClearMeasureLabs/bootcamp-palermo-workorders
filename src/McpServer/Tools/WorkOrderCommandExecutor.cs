using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// Resolves work-order state commands for MCP tool execution.
/// </summary>
internal static class WorkOrderCommandExecutor
{
    internal const string AvailableCommandsList =
        "DraftToAssignedCommand, AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand";

    internal static async Task<(WorkOrder? WorkOrder, string? Error)> LoadWorkOrderAsync(IBus bus, string workOrderNumber)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return (null, $"No work order found with number '{workOrderNumber}'.");
        }

        return (workOrder, null);
    }

    internal static async Task<(Employee? Employee, string? Error)> LoadEmployeeAsync(IBus bus, string username, string notFoundMessage)
    {
        var employee = await FindEmployeeByUsername(bus, username);
        if (employee == null)
        {
            return (null, notFoundMessage);
        }

        return (employee, null);
    }

    internal static async Task<string?> PrepareDraftToAssignedAsync(
        IBus bus,
        WorkOrder workOrder,
        string? assigneeUsername)
    {
        if (string.IsNullOrEmpty(assigneeUsername))
        {
            return "DraftToAssignedCommand requires an assigneeUsername parameter.";
        }

        var (assignee, error) = await LoadEmployeeAsync(
            bus,
            assigneeUsername,
            $"Assignee with username '{assigneeUsername}' not found.");

        if (error != null)
        {
            return error;
        }

        workOrder.Assignee = assignee;
        return null;
    }

    internal static StateCommandBase? CreateCommand(string commandName, WorkOrder workOrder, Employee user) =>
        commandName switch
        {
            "DraftToAssignedCommand" => new DraftToAssignedCommand(workOrder, user),
            "AssignedToInProgressCommand" => new AssignedToInProgressCommand(workOrder, user),
            "InProgressToAssignedCommand" => new InProgressToAssignedCommand(workOrder, user),
            "Shelve" => new InProgressToAssignedCommand(workOrder, user),
            "InProgressToCompleteCommand" => new InProgressToCompleteCommand(workOrder, user),
            "AssignedToCancelledCommand" => new AssignedToCancelledCommand(workOrder, user),
            _ => null
        };

    internal static string FormatUnknownCommand(string commandName) =>
        $"Unknown command '{commandName}'. Available commands: {AvailableCommandsList}.";

    internal static string FormatInvalidCommand(string commandName, WorkOrder workOrder, StateCommandBase command) =>
        $"Command '{commandName}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' status but the command requires '{command.GetBeginStatus().FriendlyName}' status.";

    private static async Task<Employee?> FindEmployeeByUsername(IBus bus, string username)
    {
        try
        {
            return await bus.Send(new EmployeeByUserNameQuery(username));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
