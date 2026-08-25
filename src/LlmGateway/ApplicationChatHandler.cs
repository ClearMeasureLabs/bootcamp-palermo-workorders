using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ApplicationChatHandler(ChatClientFactory factory, IToolProvider toolProvider)
    : IRequestHandler<ApplicationChatQuery, ApplicationChatResult>
{
    public async Task<ApplicationChatResult> Handle(ApplicationChatQuery request, CancellationToken cancellationToken)
    {
        var tools = await toolProvider.GetToolsAsync();
        var chatOptions = new ChatOptions { Tools = tools };
        var chatMessages = BuildChatMessages(request);

        IChatClient client = await factory.GetChatClient();
        ChatResponse response = await client.GetResponseAsync(chatMessages, chatOptions, cancellationToken);
        return ToResult(response);
    }

    /// <summary>
    /// Maps LLM <see cref="ChatResponse"/> to a remoting-safe DTO (Text only).
    /// </summary>
    internal static ApplicationChatResult ToResult(ChatResponse response)
    {
        var text = ExtractAssistantText(response);
        return new ApplicationChatResult(string.IsNullOrWhiteSpace(text) ? string.Empty : text);
    }

    /// <summary>
    /// Prefers <see cref="ChatResponse.Text"/>; falls back to last assistant message text
    /// when tool-invocation responses leave Text empty.
    /// </summary>
    internal static string ExtractAssistantText(ChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text;
        }

        return response.Messages
            .LastOrDefault(m => m.Role == ChatRole.Assistant)
            ?.Text
            ?? string.Empty;
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
                "you MUST call create-dated-work-orders ONCE before replying. " +
                "Pass creatorUsername set to the logged-in user, the assignee username, title, description, and saturdayCount. " +
                "Do not call create-work-order repeatedly. Do not only acknowledge the request. " +
                "If the assignee cannot be found, create nothing. " +
                "Your final reply MUST list every created work order number and due date using yyyy-MM-dd " +
                "(copy every line from the tool result). " +
                "Asking twice creates another full set; do not deduplicate."),
            new(ChatRole.System,
                "Be brief for ordinary questions (about 3 sentences). " +
                "When listing tool results (especially dated work orders), include EVERY item with yyyy-MM-dd dates — " +
                "listing overrides the brief-answer limit."),
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
