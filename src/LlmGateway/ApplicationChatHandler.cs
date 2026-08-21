using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ApplicationChatHandler(ChatClientFactory factory, IToolProvider toolProvider) : IRequestHandler<ApplicationChatQuery, ChatResponse>
{
    public async Task<ChatResponse> Handle(ApplicationChatQuery request, CancellationToken cancellationToken)
    {
        var tools = await toolProvider.GetToolsAsync();
        var chatOptions = new ChatOptions { Tools = tools };
        var chatMessages = BuildChatMessages(request);

        IChatClient client = await factory.GetChatClient();
        ChatResponse response = await client.GetResponseAsync(chatMessages, chatOptions, cancellationToken);
        return response;
    }

    /// <summary>
    /// Builds the system, history, and user messages for an application chat turn.
    /// </summary>
    internal static List<ChatMessage> BuildChatMessages(ApplicationChatQuery request)
    {
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful AI assistant for a work order management application. " +
                                 "You can help with general questions, look up work orders, find employees, " +
                                 "and assist with any tasks related to managing work orders."),
            new(ChatRole.System, "Limit answer to 3 sentences. Be brief"),
            new(ChatRole.System, $"Currently logged in user is {request.CurrentUsername}"),
        };

        foreach (var history in request.ChatHistory)
        {
            var role = history.Role == "user" ? ChatRole.User : ChatRole.Assistant;
            chatMessages.Add(new ChatMessage(role, history.Content));
        }

        chatMessages.Add(new ChatMessage(ChatRole.User, request.Prompt));
        return chatMessages;
    }
}
