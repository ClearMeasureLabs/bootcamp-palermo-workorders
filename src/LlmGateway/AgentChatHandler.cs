using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class AgentChatHandler(ChatClientFactory factory, WorkOrderTool workOrderTool) : IRequestHandler<AgentChatQuery, ChatResponse>
{
    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            AIFunctionFactory.Create(workOrderTool.GetWorkOrderByNumber),
            AIFunctionFactory.Create(workOrderTool.GetAllEmployees),
            AIFunctionFactory.Create(workOrderTool.GetWorkOrdersByCreatorUserName),
        ]
    };

    public async Task<ChatResponse> Handle(AgentChatQuery request, CancellationToken cancellationToken)
    {
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You help users with work orders and related questions. You can look up work orders by number, list employees, and list work orders created by a user (by username). Be concise."),
            new(ChatRole.System, $"The user you are helping is logged in as {request.UserName}."),
            new(ChatRole.System, "Limit answer to 3 sentences. Be brief."),
            new(ChatRole.User, request.Prompt)
        };

        IChatClient client = await factory.GetChatClient();
        ChatResponse response = await client.GetResponseAsync(chatMessages, _chatOptions);
        return response;
    }
}
