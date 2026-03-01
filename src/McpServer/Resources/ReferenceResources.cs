using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Resources;

[McpServerResourceType]
public class ReferenceResources
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [McpServerResource(UriTemplate = "churchbulletin://reference/work-order-statuses", Name = "work-order-statuses"),
     Description("All valid work order statuses with their code, key, and friendly name.")]
    public static string GetWorkOrderStatuses()
    {
        var statuses = WorkOrderStatus.GetAllItems().Select(s => new
        {
            s.Code,
            s.Key,
            s.FriendlyName
        }).ToArray();

        return JsonSerializer.Serialize(statuses, JsonOptions);
    }

    [McpServerResource(UriTemplate = "churchbulletin://reference/roles", Name = "roles"),
     Description("All defined roles with their permissions.")]
    public static string GetRoles()
    {
        var roles = new[]
        {
            new { Name = "Manager", CanCreateWorkOrder = true, CanFulfillWorkOrder = false },
            new { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true },
            new { Name = "Admin", CanCreateWorkOrder = true, CanFulfillWorkOrder = true }
        };

        return JsonSerializer.Serialize(roles, JsonOptions);
    }

    [McpServerResource(UriTemplate = "churchbulletin://reference/status-transitions", Name = "status-transitions"),
     Description("Valid state transitions for work orders. Maps each status to the commands that can be executed and their target status.")]
    public static string GetStatusTransitions()
    {
        var transitions = new
        {
            Draft = new[]
            {
                new { Command = "SaveDraftCommand", TargetStatus = "Draft" },
                new { Command = "DraftToAssignedCommand", TargetStatus = "Assigned" }
            },
            Assigned = new[]
            {
                new { Command = "AssignedToInProgressCommand", TargetStatus = "InProgress" }
            },
            InProgress = new[]
            {
                new { Command = "InProgressToCompleteCommand", TargetStatus = "Complete" }
            },
            Complete = System.Array.Empty<object>()
        };

        return JsonSerializer.Serialize(transitions, JsonOptions);
    }
}
