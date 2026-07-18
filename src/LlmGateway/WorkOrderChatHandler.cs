using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkOrderChatHandler(ChatClientFactory factory, WorkOrderTool workOrderTool) : IRequestHandler<WorkOrderChatQuery, ChatResponse>
{
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
            new(ChatRole.System, $"Limit answer to 3 sentences unless listing data. When listing items, include ALL items from the tool response. Be brief otherwise."),
            new(ChatRole.User, prompt)
            
        };

        IChatClient client = await factory.GetChatClient();
        ChatResponse responseAsync = await client.GetResponseAsync(chatMessages, _chatOptions);
        return responseAsync;
    }
}