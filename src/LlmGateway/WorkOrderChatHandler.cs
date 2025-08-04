using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkOrderChatHandler(IChatClient client) : IRequestHandler<WorkOrderChatQuery, ChatResponse>
{
    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            AIFunctionFactory.Create(WorkOrderTool.GetWorkOrderByNumber),
        ]
    };

    public async Task<ChatResponse> Handle(WorkOrderChatQuery request, CancellationToken cancellationToken)
    {
        Console.Write("Enter your prompt: ");
        string prompt = request.Prompt;
        ChatResponse responseAsync = await client.GetResponseAsync(prompt, _chatOptions);
        return responseAsync;
    }
}