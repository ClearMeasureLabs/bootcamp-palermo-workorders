using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class WorkOrderTools
{
    [McpServerTool(Name = "list-work-orders"), Description("Lists all work orders, optionally filtered by status. Valid statuses: Draft, Assigned, InProgress, Complete.")]
    public static async Task<string> ListWorkOrders(
        IBus bus,
        [Description("Optional status filter (Draft, Assigned, InProgress, Complete)")] string? status = null)
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

    [McpServerTool(Name = "create-work-order"), Description("Creates a new draft work order. Requires a title, description, and the username of the creator. Optionally accepts a room number and due date (yyyy-MM-dd).")]
    public static async Task<string> CreateWorkOrder(
        IBus bus,
        IWorkOrderNumberGenerator numberGenerator,
        [Description("Title of the work order")] string title,
        [Description("Description of the work order")] string description,
        [Description("Username of the employee creating the work order")] string creatorUsername,
        [Description("Optional room number or location for the work order")] string? roomNumber = null,
        [Description("Optional due date as yyyy-MM-dd")] string? dueDate = null)
    {
        try
        {
            var creator = await FindEmployeeByUsername(bus, creatorUsername);
            if (creator == null)
            {
                return $"Employee with username '{creatorUsername}' not found.";
            }

            if (!DatedWorkOrderScheduling.TryParseOptionalDueDate(dueDate, out var parsedDueDate, out var dueDateError))
            {
                return dueDateError!;
            }

            var workOrder = new WorkOrder
            {
                Title = title,
                Description = description,
                Creator = creator,
                Status = WorkOrderStatus.Draft,
                Number = numberGenerator.GenerateNumber(),
                RoomNumber = roomNumber,
                DueDate = parsedDueDate
            };

            var command = new SaveDraftCommand(workOrder, creator);
            var result = await bus.Send(command);

            return JsonSerializer.Serialize(FormatWorkOrderDetail(result.WorkOrder),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error creating work order: {ex.Message}";
        }
    }

    [McpServerTool(Name = "create-dated-work-orders"), Description(
        "Creates multiple dated assigned work orders in ONE transaction. " +
        "Use this for scheduling (e.g. next N Saturdays). " +
        "Looks up the assignee first; if missing, creates nothing. " +
        "Pass dueDates as comma-separated yyyy-MM-dd values, or omit dueDates and set saturdayCount to create consecutive Saturdays in America/Chicago starting with the coming Saturday.")]
    public static async Task<string> CreateDatedWorkOrders(
        IBus bus,
        TimeProvider timeProvider,
        [Description("Username of the employee creating the work orders (logged-in user)")] string creatorUsername,
        [Description("Username of the assignee (e.g. gwillie)")] string assigneeUsername,
        [Description("Title for each work order")] string title,
        [Description("Description for each work order")] string description,
        [Description("Optional comma-separated due dates as yyyy-MM-dd")] string? dueDates = null,
        [Description("When dueDates is omitted, number of consecutive Chicago Saturdays to schedule (default 10)")] int saturdayCount = 10)
    {
        try
        {
            var (dates, resolveError) = DatedWorkOrderScheduling.ResolveDueDates(timeProvider, dueDates, saturdayCount);
            if (resolveError != null)
            {
                return resolveError;
            }

            var result = await bus.Send(new CreateDatedWorkOrdersCommand(
                creatorUsername,
                assigneeUsername,
                title,
                description,
                dates));

            return DatedWorkOrderScheduling.FormatResult(result);
        }
        catch (Exception ex)
        {
            return $"Error creating dated work orders: {ex.Message}";
        }
    }

    [McpServerTool(Name = "execute-work-order-command"), Description("Executes a state command on a work order. Available commands: DraftToAssignedCommand (requires assigneeUsername), AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand.")]
    public static Task<string> ExecuteWorkOrderCommand(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("The command name (e.g., DraftToAssignedCommand)")] string commandName,
        [Description("Username of the employee executing the command")] string executingUsername,
        [Description("Username of the employee to assign the work order to (required for DraftToAssignedCommand)")] string? assigneeUsername = null) =>
        WorkOrderCommandExecutor.ExecuteCommandAsync(
            bus,
            workOrderNumber,
            commandName,
            executingUsername,
            assigneeUsername);

    [McpServerTool(Name = "list-work-order-attachments"), Description("Lists all attachment metadata for a given work order by its number.")]
    public static async Task<string> ListWorkOrderAttachments(
        IBus bus,
        [Description("The work order number")] string workOrderNumber)
    {
        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return $"No work order found with number '{workOrderNumber}'.";
        }

        var attachments = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));
        return JsonSerializer.Serialize(attachments.Select(a => new
        {
            a.Id,
            a.FileName,
            a.ContentType,
            a.FileSize,
            UploadedBy = a.UploadedBy?.GetFullName(),
            UploadedByUsername = a.UploadedBy?.UserName,
            a.UploadedDate
        }).ToArray(), new JsonSerializerOptions { WriteIndented = true });
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

    private static object FormatWorkOrderSummary(WorkOrder wo) => new
    {
        wo.Number,
        wo.Title,
        Status = wo.Status.FriendlyName,
        Creator = wo.Creator?.GetFullName(),
        Assignee = wo.Assignee?.GetFullName(),
        DueDate = wo.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };

    internal static object FormatWorkOrderDetail(WorkOrder wo) => new
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
        wo.CompletedDate,
        DueDate = wo.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };
}
