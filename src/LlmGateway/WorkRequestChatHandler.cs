using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class WorkRequestChatHandler(ChatClientFactory factory, WorkRequestTool workRequestTool) : IRequestHandler<WorkRequestChatQuery, ChatResponse>
{
    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            AIFunctionFactory.Create(workRequestTool.GetWorkRequestByNumber),
            AIFunctionFactory.Create(workRequestTool.GetAllEmployees)
        ]
    };

    public async Task<ChatResponse> Handle(WorkRequestChatQuery request, CancellationToken cancellationToken)
    {
        string prompt = request.Prompt;
        var chatMessages = new List<ChatMessage>()
        {
            new(ChatRole.System, "You help user's do the work specified in the WorkRequest"),
            new(ChatRole.System, $"Work Request number is {request.CurrentWorkRequest.Number}"),
            new(ChatRole.System, $"Limit answer to 3 sentences unless listing data. When listing items, include ALL items from the tool response. Be brief otherwise."),
            new(ChatRole.User, prompt)
            
        };

        IChatClient client = await factory.GetChatClient();
        ChatResponse responseAsync = await client.GetResponseAsync(chatMessages, _chatOptions);
        return responseAsync;
    }
}