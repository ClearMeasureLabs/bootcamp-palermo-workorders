using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     AI agent responsible for evaluating work orders for automatic cancellation
/// </summary>
public class WorkOrderEvaluationAgent(
    ChatClientFactory chatClientFactory,
    IBus bus,
    ILogger<WorkOrderEvaluationAgent> logger)
{
    private readonly IBus _bus = bus;

    /// <summary>
    ///     Evaluates a work order to determine if it should be automatically cancelled
    /// </summary>
    public async Task<bool> ShouldCancelWorkOrderAsync(WorkOrder workOrder)
    {
        try
        {
            var chatClient = await chatClientFactory.GetChatClient();

            var systemPrompt = """
                               You are an AI agent responsible for evaluating work orders for automatic cancellation.
                               Analyze this work order and determine if it should be cancelled:

                               Decision criteria:
                               - Ignore and skip if work order title is 'from automation'
                               - Cancel if assigned more than 24 hours ago
                               - Cancel if description contains keywords like 'test', 'demo', or 'temporary'
                               - Cancel if room number is 'TEST' or similar

                               Respond with only 'YES' if the work order should be cancelled, or 'NO' if it should not be cancelled.
                               """;

            var workOrderInfo = $"""
                                 Work Order: {workOrder.Number}
                                 Title: {workOrder.Title}
                                 Description: {workOrder.Description}
                                 Room Number: {workOrder.RoomNumber}
                                 Assigned Date: {workOrder.AssignedDate}
                                 Creator: {workOrder.Creator?.GetFullName()}
                                 Assignee: {workOrder.Assignee?.GetFullName()}
                                 """;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, workOrderInfo)
            };

            var response = await chatClient.GetResponseAsync(messages);
            var decision = response.Text?.Trim().ToUpperInvariant();

            logger.LogInformation("AI evaluation for WorkOrder {WorkOrderNumber}: {Decision}",
                workOrder.Number, decision);

            return decision == "YES";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating WorkOrder {WorkOrderNumber} for cancellation",
                workOrder.Number);
            return false;
        }
    }
}