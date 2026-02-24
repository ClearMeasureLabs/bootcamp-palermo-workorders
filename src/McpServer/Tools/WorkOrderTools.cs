using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class WorkOrderTools
{
    [McpServerTool(Name = "list-work-orders"), Description("Lists all work orders, optionally filtered by status. Valid statuses: Draft, Assigned, InProgress, Complete, Cancelled.")]
    public static async Task<string> ListWorkOrders(
        IBus bus,
        [Description("Optional status filter (Draft, Assigned, InProgress, Complete, Cancelled)")] string? status = null)
    {
        var query = new WorkOrderSpecificationQuery();
        if (!string.IsNullOrEmpty(status))
        {
            query.MatchStatus(WorkOrderStatus.FromKey(status));
        }

        var workOrders = await bus.Send(query);
        return JsonSerializer.Serialize(workOrders.Select(FormatWorkOrderSummary).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "get-work-order"), Description("Retrieves a single work order by its number, including full details.")]
    public static async Task<string> GetWorkOrder(
        IBus bus,
        [Description("The work order number")] string workOrderNumber)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return $"No work order found with number '{workOrderNumber}'.";
        }

        return JsonSerializer.Serialize(FormatWorkOrderDetail(workOrder),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "create-work-order"), Description("Creates a new draft work order. Requires a title, description, and the username of the creator.")]
    public static async Task<string> CreateWorkOrder(
        IBus bus,
        [Description("Title of the work order")] string title,
        [Description("Description of the work order")] string description,
        [Description("Username of the employee creating the work order")] string creatorUsername)
    {
        var creator = await bus.Send(new EmployeeByUserNameQuery(creatorUsername));
        if (creator == null)
        {
            return $"Employee with username '{creatorUsername}' not found.";
        }

        var workOrder = new WorkOrder
        {
            Title = title,
            Description = description,
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        var command = new SaveDraftCommand(workOrder, creator);
        var result = await bus.Send(command);

        return JsonSerializer.Serialize(FormatWorkOrderDetail(result.WorkOrder),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "execute-work-order-command"), Description("Executes a state command on a work order. Available commands: DraftToAssignedCommand, AssignedToInProgressCommand, InProgressToCompleteCommand, AssignedToCancelledCommand, InProgressToCancelledCommand, InProgressToAssigned.")]
    public static async Task<string> ExecuteWorkOrderCommand(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("The command name (e.g., DraftToAssignedCommand)")] string commandName,
        [Description("Username of the employee executing the command")] string executingUsername)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return $"No work order found with number '{workOrderNumber}'.";
        }

        var user = await bus.Send(new EmployeeByUserNameQuery(executingUsername));
        if (user == null)
        {
            return $"Employee with username '{executingUsername}' not found.";
        }

        StateCommandBase? command = commandName switch
        {
            "DraftToAssignedCommand" => new DraftToAssignedCommand(workOrder, user),
            "AssignedToInProgressCommand" => new AssignedToInProgressCommand(workOrder, user),
            "InProgressToCompleteCommand" => new InProgressToCompleteCommand(workOrder, user),
            "AssignedToCancelledCommand" => new AssignedToCancelledCommand(workOrder, user),
            "InProgressToCancelledCommand" => new InProgressToCancelledCommand(workOrder, user),
            "InProgressToAssigned" => new InProgressToAssigned(workOrder, user),
            _ => null
        };

        if (command == null)
        {
            return $"Unknown command '{commandName}'. Available commands: DraftToAssignedCommand, AssignedToInProgressCommand, InProgressToCompleteCommand, AssignedToCancelledCommand, InProgressToCancelledCommand, InProgressToAssigned.";
        }

        if (!command.IsValid())
        {
            return $"Command '{commandName}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' status but the command requires '{command.GetBeginStatus().FriendlyName}' status.";
        }

        var result = await bus.Send(command);
        return JsonSerializer.Serialize(FormatWorkOrderDetail(result.WorkOrder),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "update-work-order-description"), Description("Updates the description of an existing work order.")]
    public static async Task<string> UpdateWorkOrderDescription(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("The new description")] string newDescription,
        [Description("Username of the employee making the update")] string updatingUsername)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return $"No work order found with number '{workOrderNumber}'.";
        }

        var user = await bus.Send(new EmployeeByUserNameQuery(updatingUsername));
        if (user == null)
        {
            return $"Employee with username '{updatingUsername}' not found.";
        }

        workOrder.Description = newDescription;
        var command = new UpdateDescriptionCommand(workOrder, user);

        if (!command.IsValid())
        {
            return $"User '{updatingUsername}' is not authorized to update this work order's description.";
        }

        var result = await bus.Send(command);
        return JsonSerializer.Serialize(FormatWorkOrderDetail(result.WorkOrder),
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static object FormatWorkOrderSummary(WorkOrder wo) => new
    {
        wo.Number,
        wo.Title,
        Status = wo.Status.FriendlyName,
        Creator = wo.Creator?.GetFullName(),
        Assignee = wo.Assignee?.GetFullName()
    };

    private static object FormatWorkOrderDetail(WorkOrder wo) => new
    {
        wo.Number,
        wo.Title,
        wo.Description,
        Status = wo.Status.FriendlyName,
        wo.RoomNumber,
        Creator = wo.Creator?.GetFullName(),
        CreatorUsername = wo.Creator?.UserName,
        Assignee = wo.Assignee?.GetFullName(),
        AssigneeUsername = wo.Assignee?.UserName,
        wo.CreatedDate,
        wo.AssignedDate,
        wo.CompletedDate
    };
}
