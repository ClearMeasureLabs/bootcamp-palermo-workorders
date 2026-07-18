using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class WorkRequestTools
{
    [McpServerTool(Name = "list-work-requests"), Description("Lists all work requests, optionally filtered by status. Valid statuses: Draft, Assigned, InProgress, Complete.")]
    public static async Task<string> ListWorkRequests(
        IBus bus,
        [Description("Optional status filter (Draft, Assigned, InProgress, Complete)")] string? status = null)
    {
        var query = new WorkRequestSpecificationQuery();
        if (!string.IsNullOrEmpty(status))
        {
            query.MatchStatus(WorkRequestStatus.FromKey(status));
        }

        var workRequests = await bus.Send(query);
        return JsonSerializer.Serialize(workRequests.Select(FormatWorkRequestSummary).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "get-work-request"), Description("Retrieves a single work request by its number, including full details.")]
    public static async Task<string> GetWorkRequest(
        IBus bus,
        [Description("The work request number")] string workRequestNumber)
    {
        var workRequest = await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));
        if (workRequest == null)
        {
            return $"No work request found with number '{workRequestNumber}'.";
        }

        return JsonSerializer.Serialize(FormatWorkRequestDetail(workRequest),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "create-work-request"), Description("Creates a new draft work request. Requires a title, description, and the username of the creator. Optionally accepts a room number for the location.")]
    public static async Task<string> CreateWorkRequest(
        IBus bus,
        IWorkRequestNumberGenerator numberGenerator,
        [Description("Title of the work request")] string title,
        [Description("Description of the work request")] string description,
        [Description("Username of the employee creating the work request")] string creatorUsername,
        [Description("Optional room number or location for the work request")] string? roomNumber = null)
    {
        try
        {
            var creator = await FindEmployeeByUsername(bus, creatorUsername);
            if (creator == null)
            {
                return $"Employee with username '{creatorUsername}' not found.";
            }

            var workRequest = new WorkRequest
            {
                Title = title,
                Description = description,
                Creator = creator,
                Status = WorkRequestStatus.Draft,
                Number = numberGenerator.GenerateNumber(),
                RoomNumber = roomNumber
            };

            var command = new SaveDraftCommand(workRequest, creator);
            var result = await bus.Send(command);

            return JsonSerializer.Serialize(FormatWorkRequestDetail(result.WorkRequest),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error creating work request: {ex.Message}";
        }
    }

    [McpServerTool(Name = "execute-work-request-command"), Description("Executes a state command on a work request. Available commands: DraftToAssignedCommand (requires assigneeUsername), AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand.")]
    public static async Task<string> ExecuteWorkRequestCommand(
        IBus bus,
        [Description("The work request number")] string workRequestNumber,
        [Description("The command name (e.g., DraftToAssignedCommand)")] string commandName,
        [Description("Username of the employee executing the command")] string executingUsername,
        [Description("Username of the employee to assign the work request to (required for DraftToAssignedCommand)")] string? assigneeUsername = null)
    {
        var workRequest = await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));
        if (workRequest == null)
        {
            return $"No work request found with number '{workRequestNumber}'.";
        }

        var user = await FindEmployeeByUsername(bus, executingUsername);
        if (user == null)
        {
            return $"Employee with username '{executingUsername}' not found.";
        }

        if (commandName == "DraftToAssignedCommand")
        {
            if (string.IsNullOrEmpty(assigneeUsername))
            {
                return "DraftToAssignedCommand requires an assigneeUsername parameter.";
            }

            var assignee = await FindEmployeeByUsername(bus, assigneeUsername);
            if (assignee == null)
            {
                return $"Assignee with username '{assigneeUsername}' not found.";
            }

            workRequest.Assignee = assignee;
        }

        StateCommandBase? command = commandName switch
        {
            "DraftToAssignedCommand" => new DraftToAssignedCommand(workRequest, user),
            "AssignedToInProgressCommand" => new AssignedToInProgressCommand(workRequest, user),
            "InProgressToAssignedCommand" => new InProgressToAssignedCommand(workRequest, user),
            "Shelve" => new InProgressToAssignedCommand(workRequest, user),
            "InProgressToCompleteCommand" => new InProgressToCompleteCommand(workRequest, user),
            "AssignedToCancelledCommand" => new AssignedToCancelledCommand(workRequest, user),
            _ => null
        };

        if (command == null)
        {
            return $"Unknown command '{commandName}'. Available commands: DraftToAssignedCommand, AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand.";
        }

        if (!command.IsValid())
        {
            return $"Command '{commandName}' cannot be executed. Work request is in '{workRequest.Status.FriendlyName}' status but the command requires '{command.GetBeginStatus().FriendlyName}' status.";
        }

        var result = await bus.Send(command);
        return JsonSerializer.Serialize(FormatWorkRequestDetail(result.WorkRequest),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "list-work-request-attachments"), Description("Lists all attachment metadata for a given work request by its number.")]
    public static async Task<string> ListWorkRequestAttachments(
        IBus bus,
        [Description("The work request number")] string workRequestNumber)
    {
        var workRequest = await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));
        if (workRequest == null)
        {
            return $"No work request found with number '{workRequestNumber}'.";
        }

        var attachments = await bus.Send(new WorkRequestAttachmentsQuery(workRequest.Id));
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

    private static object FormatWorkRequestSummary(WorkRequest wo) => new
    {
        wo.Number,
        wo.Title,
        Status = wo.Status.FriendlyName,
        Creator = wo.Creator?.GetFullName(),
        Assignee = wo.Assignee?.GetFullName()
    };

    private static object FormatWorkRequestDetail(WorkRequest wo) => new
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
