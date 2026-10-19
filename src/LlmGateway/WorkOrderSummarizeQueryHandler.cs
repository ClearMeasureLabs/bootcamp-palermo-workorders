using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ClientModel;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Asks the configured chat client for a one-paragraph summary of a work order.
/// </summary>
public class WorkOrderSummarizeQueryHandler(
    ChatClientFactory factory,
    ILogger<WorkOrderSummarizeQueryHandler> logger)
    : IRequestHandler<WorkOrderSummarizeQuery, WorkOrderSummarizeResult>
{
    internal const string FriendlyUnavailableMessage =
        "AI summary is unavailable right now. Please try again later.";

    public async Task<WorkOrderSummarizeResult> Handle(
        WorkOrderSummarizeQuery request,
        CancellationToken cancellationToken)
    {
        var client = await TryGetChatClientAsync();
        if (client is null)
        {
            return new WorkOrderSummarizeResult(FriendlyUnavailableMessage);
        }

        return await RequestSummaryAsync(client, request, cancellationToken);
    }

    private async Task<IChatClient?> TryGetChatClientAsync()
    {
        try
        {
            return await factory.GetChatClient();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM chat client unavailable for work order summarize");
            return null;
        }
    }

    private async Task<WorkOrderSummarizeResult> RequestSummaryAsync(
        IChatClient client,
        WorkOrderSummarizeQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            ChatResponse response = await client.GetResponseAsync(
                BuildChatMessages(request), cancellationToken: cancellationToken);
            var text = response.Text.Trim();
            return new WorkOrderSummarizeResult(
                string.IsNullOrWhiteSpace(text) ? FriendlyUnavailableMessage : text);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ClientResultException)
        {
            logger.LogWarning(ex, "LLM provider refused or timed out during work order summarize");
            return new WorkOrderSummarizeResult(FriendlyUnavailableMessage);
        }
    }

    /// <summary>
    /// Builds the system and user messages for a work-order summarize turn.
    /// </summary>
    internal static List<ChatMessage> BuildChatMessages(WorkOrderSummarizeQuery request)
    {
        return
        [
            new ChatMessage(ChatRole.System,
                "Summarize the following work order in one short paragraph. " +
                "Return only the summary text, nothing else."),
            new ChatMessage(ChatRole.User,
                $"Title: {request.Title}\nDescription: {request.Description}\nInstructions: {request.Instructions}")
        ];
    }
}
