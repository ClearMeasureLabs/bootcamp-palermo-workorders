using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderCancellationAgent
{
    private readonly ChatClientFactory _chatClientFactory;

    public WorkOrderCancellationAgent(ChatClientFactory chatClientFactory)
    {
        _chatClientFactory = chatClientFactory;
    }

    public async Task<bool> ShouldCancelWorkOrder(WorkOrder workOrder)
    {
        var client = await _chatClientFactory.GetChatClient();

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are an AI agent responsible for evaluating work orders for automatic cancellation."),
            new(ChatRole.User, BuildPrompt(workOrder))
        };

        var response = await client.GetResponseAsync(chatMessages);

        var result = response.Text?.Trim().ToLowerInvariant();
        return result?.StartsWith("yes") == true || result?.Contains("cancel") == true;
    }

    private static string BuildPrompt(WorkOrder workOrder)
    {
        var assignedHoursAgo = workOrder.AssignedDate.HasValue 
            ? Math.Round((DateTime.UtcNow - workOrder.AssignedDate.Value).TotalHours, 1)
            : 0;

        return $@"Analyze this work order and determine if it should be cancelled:

Work Order Details:
- Number: {workOrder.Number}
- Title: {workOrder.Title}
- Description: {workOrder.Description}
- Room Number: {workOrder.RoomNumber}
- Status: {workOrder.Status?.FriendlyName}
- Hours since assigned: {assignedHoursAgo}

Decision criteria:
- Cancel if assigned more than 24 hours ago
- Cancel if description contains keywords like 'test', 'demo', or 'temporary'
- Cancel if room number is 'TEST' or similar

Respond with only 'YES' if the work order should be cancelled, or 'NO' if it should remain assigned.";
    }
}