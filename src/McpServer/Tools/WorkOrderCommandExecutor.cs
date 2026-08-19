using System.Text.Json;
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

    private static readonly IReadOnlyDictionary<string, Func<WorkOrder, Employee, StateCommandBase>> CommandFactories =
        new Dictionary<string, Func<WorkOrder, Employee, StateCommandBase>>(StringComparer.Ordinal)
        {
            ["DraftToAssignedCommand"] = (workOrder, user) => new DraftToAssignedCommand(workOrder, user),
            ["AssignedToInProgressCommand"] = (workOrder, user) => new AssignedToInProgressCommand(workOrder, user),
            ["InProgressToAssignedCommand"] = (workOrder, user) => new InProgressToAssignedCommand(workOrder, user),
            ["Shelve"] = (workOrder, user) => new InProgressToAssignedCommand(workOrder, user),
            ["InProgressToCompleteCommand"] = (workOrder, user) => new InProgressToCompleteCommand(workOrder, user),
            ["AssignedToCancelledCommand"] = (workOrder, user) => new AssignedToCancelledCommand(workOrder, user),
        };

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
        CommandFactories.TryGetValue(commandName, out var factory) ? factory(workOrder, user) : null;

    internal static string FormatUnknownCommand(string commandName) =>
        $"Unknown command '{commandName}'. Available commands: {AvailableCommandsList}.";

    internal static string FormatInvalidCommand(string commandName, WorkOrder workOrder, StateCommandBase command) =>
        $"Command '{commandName}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' status but the command requires '{command.GetBeginStatus().FriendlyName}' status.";

    internal static async Task<string> ExecuteCommandAsync(
        IBus bus,
        string workOrderNumber,
        string commandName,
        string executingUsername,
        string? assigneeUsername)
    {
        var (workOrder, workOrderError) = await LoadWorkOrderAsync(bus, workOrderNumber);
        if (workOrderError != null)
        {
            return workOrderError;
        }

        var (user, userError) = await LoadEmployeeAsync(
            bus,
            executingUsername,
            $"Employee with username '{executingUsername}' not found.");
        if (userError != null)
        {
            return userError;
        }

        if (commandName == "DraftToAssignedCommand"
            && await PrepareDraftToAssignedAsync(bus, workOrder!, assigneeUsername) is { } assigneeError)
        {
            return assigneeError;
        }

        var command = CreateCommand(commandName, workOrder!, user!);
        if (command == null)
        {
            return FormatUnknownCommand(commandName);
        }

        if (!command.IsValid())
        {
            return FormatInvalidCommand(commandName, workOrder!, command);
        }

        var result = await bus.Send(command);
        return JsonSerializer.Serialize(
            WorkOrderTools.FormatWorkOrderDetail(result.WorkOrder),
            new JsonSerializerOptions { WriteIndented = true });
    }

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
