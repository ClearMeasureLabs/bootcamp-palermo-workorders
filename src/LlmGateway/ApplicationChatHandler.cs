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
            new(ChatRole.System,
                "When asked to schedule multiple dated work orders (for example next N Saturdays), " +
                "call create-dated-work-orders ONCE with creatorUsername set to the logged-in user, " +
                "the assignee username, title, description, and saturdayCount. " +
                "Do not call create-work-order repeatedly. " +
                "If the assignee cannot be found, create nothing. " +
                "In your reply, list every created work order number and due date (yyyy-MM-dd). " +
                "Asking twice creates another full set; do not deduplicate."),
            new(ChatRole.System, "Limit answer to 3 sentences unless listing data. When listing items, include ALL items from the tool response. Be brief otherwise."),
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
