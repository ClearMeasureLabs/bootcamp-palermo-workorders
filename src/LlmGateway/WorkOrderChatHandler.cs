using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ClientModel;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkOrderChatHandler(ChatClientFactory factory, WorkOrderTool workOrderTool, ILogger<WorkOrderChatHandler> logger) : IRequestHandler<WorkOrderChatQuery, ChatResponse>
{
    internal const string FriendlyProviderErrorMessage =
        "I couldn't process that request. Please rephrase and try again.";

    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            AIFunctionFactory.Create(workOrderTool.GetWorkOrderByNumber),
            AIFunctionFactory.Create(workOrderTool.GetAllEmployees)
        ]
    };

    public async Task<ChatResponse> Handle(WorkOrderChatQuery request, CancellationToken cancellationToken)
    {
        string prompt = request.Prompt;
        var chatMessages = new List<ChatMessage>()
        {
            new(ChatRole.System, "You help user's do the work specified in the WorkOrder"),
            new(ChatRole.System, $"Work Order number is {request.CurrentWorkOrder.Number}"),
            new(ChatRole.System, "Limit answer to 3 sentences unless listing data. When listing items, include ALL items from the tool response. Be brief otherwise."),
            new(ChatRole.User, prompt)
        };

        IChatClient client = await factory.GetChatClient();
        try
        {
            ChatResponse response = await client.GetResponseAsync(chatMessages, _chatOptions, cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ClientResultException)
        {
            var statusCode = ex is ClientResultException cre ? cre.Status : (int?)null;
            logger.LogWarning(ex, "LLM provider refused or timed out: {StatusCode} {Reason}",
                statusCode, ex.Message);
            return new ChatResponse([new ChatMessage(ChatRole.Assistant, FriendlyProviderErrorMessage)]);
        }
    }
}