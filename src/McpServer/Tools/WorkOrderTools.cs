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

    [McpServerTool(Name = "create-work-order"), Description("Creates a new draft work order. Requires a title, description, and the username of the creator. Optionally accepts a room number, due date (yyyy-MM-dd), and instructions.")]
    public static async Task<string> CreateWorkOrder(
        IBus bus,
        IWorkOrderNumberGenerator numberGenerator,
        [Description("Title of the work order")] string title,
        [Description("Description of the work order")] string description,
        [Description("Username of the employee creating the work order")] string creatorUsername,
        [Description("Optional room number or location for the work order")] string? roomNumber = null,
        [Description("Optional due date as yyyy-MM-dd")] string? dueDate = null,
        [Description("Optional instructions for the work order")] string? instructions = null)
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
                Instructions = instructions,
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

    [McpServerTool(Name = "save-work-order"), Description(
        "Saves title, description, instructions, room, and due date on an existing draft work order without changing status. " +
        "Requires the work order number and executing username (the creator). " +
        "Optional patches: omit a field to leave it unchanged; empty description/instructions/room persists empty; empty due date clears.")]
    public static async Task<string> SaveWorkOrder(
        IBus bus,
        [Description("The work order number")] string workOrderNumber,
        [Description("Username of the employee executing the save (must be the creator)")] string executingUsername,
        [Description("Optional title patch")] string? title = null,
        [Description("Optional description patch")] string? description = null,
        [Description("Optional instructions patch")] string? instructions = null,
        [Description("Optional room number patch")] string? roomNumber = null,
        [Description("Optional due date patch as yyyy-MM-dd; empty clears")] string? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(workOrderNumber))
        {
            return "Work order number is required.";
        }

        var workOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        if (workOrder == null)
        {
            return $"No work order found with number '{workOrderNumber}'.";
        }

        var executingUser = await FindEmployeeByUsername(bus, executingUsername);
        if (executingUser == null)
        {
            return $"Employee with username '{executingUsername}' not found.";
        }

        var validationError = ValidateSave(workOrder, executingUser, title, dueDate);
        if (validationError != null)
        {
            return validationError;
        }

        ApplySavePatches(workOrder, title, description, instructions, roomNumber, dueDate);

        var result = await bus.Send(new SaveDraftCommand(workOrder, executingUser));
        return JsonSerializer.Serialize(FormatWorkOrderDetail(result.WorkOrder),
            new JsonSerializerOptions { WriteIndented = true });
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

    private static string? ValidateSave(
        WorkOrder workOrder,
        Employee executingUser,
        string? title,
        string? dueDate)
    {
        if (!new SaveDraftCommand(workOrder, executingUser).IsValid())
        {
            return FormatInvalidSaveCommand(workOrder);
        }

        if (IsBlankPatch(title))
        {
            return "The Title field is required.";
        }

        if (IsInvalidDueDatePatch(dueDate))
        {
            return $"Invalid due date '{dueDate}'. Use yyyy-MM-dd.";
        }

        return null;
    }

    private static bool IsBlankPatch(string? value) => value != null && string.IsNullOrWhiteSpace(value);

    private static bool IsInvalidDueDatePatch(string? dueDate) =>
        dueDate != null && !string.IsNullOrWhiteSpace(dueDate) && !TryParseDueDatePatch(dueDate, out _);

    private static bool TryParseDueDatePatch(string dueDate, out DateOnly parsedDueDate) =>
        DateOnly.TryParseExact(dueDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out parsedDueDate);

    private static void ApplySavePatches(
        WorkOrder workOrder,
        string? title,
        string? description,
        string? instructions,
        string? roomNumber,
        string? dueDate)
    {
        ApplyTitlePatch(workOrder, title);
        ApplyDescriptionPatch(workOrder, description);
        ApplyInstructionsPatch(workOrder, instructions);
        ApplyRoomNumberPatch(workOrder, roomNumber);
        ApplyDueDatePatch(workOrder, dueDate);
    }

    private static void ApplyTitlePatch(WorkOrder workOrder, string? title)
    {
        if (title != null)
        {
            workOrder.Title = title;
        }
    }

    private static void ApplyDescriptionPatch(WorkOrder workOrder, string? description)
    {
        if (description != null)
        {
            workOrder.Description = BlankToEmpty(description);
        }
    }

    private static void ApplyInstructionsPatch(WorkOrder workOrder, string? instructions)
    {
        if (instructions != null)
        {
            workOrder.Instructions = BlankToEmpty(instructions);
        }
    }

    private static void ApplyRoomNumberPatch(WorkOrder workOrder, string? roomNumber)
    {
        if (roomNumber != null)
        {
            workOrder.RoomNumber = TruncateRoomNumber(BlankToEmpty(roomNumber));
        }
    }

    private static void ApplyDueDatePatch(WorkOrder workOrder, string? dueDate)
    {
        if (dueDate != null)
        {
            workOrder.DueDate = ParseDueDatePatch(dueDate);
        }
    }

    private static DateOnly? ParseDueDatePatch(string dueDate)
    {
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return null;
        }

        TryParseDueDatePatch(dueDate, out var parsedDueDate);
        return parsedDueDate;
    }

    private static string BlankToEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value;

    private static string TruncateRoomNumber(string roomNumber) =>
        roomNumber.Length <= WorkOrder.RoomNumberMaxLength
            ? roomNumber
            : roomNumber[..WorkOrder.RoomNumberMaxLength];

    private static string FormatInvalidSaveCommand(WorkOrder workOrder) =>
        $"Command '{SaveDraftCommand.Name}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' status but the command requires '{WorkOrderStatus.Draft.FriendlyName}' status.";

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
