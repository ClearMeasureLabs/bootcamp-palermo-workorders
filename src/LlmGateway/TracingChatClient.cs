using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// A delegating chat client that adds distributed tracing spans around chat operations.
/// </summary>
public class TracingChatClient(IChatClient innerClient, ChatClientConfig config) : DelegatingChatClient(innerClient)
{
    internal static readonly ActivitySource ActivitySource = new("ChurchBulletin.LlmGateway", "1.0.0");

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var promptActivity = StartActivity("ChatClient.GetResponseAsync Request");
        promptActivity?.SetTag("chat.prompt", GetLastUserMessage(messages));
        promptActivity?.SetStatus(ActivityStatusCode.Ok);

        using var responseActivity = StartActivity("ChatClient.GetResponseAsync Response");

        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            responseActivity?.SetTag("chat.model", response.ModelId);
            responseActivity?.SetTag("chat.response", response.Text);
            responseActivity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            responseActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            responseActivity?.AddEvent(new ActivityEvent("exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message }
                }));
            throw;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var promptActivity = StartActivity("ChatClient.GetStreamingResponseAsync Request");
        promptActivity?.SetTag("chat.prompt", GetLastUserMessage(messages));
        promptActivity?.SetStatus(ActivityStatusCode.Ok);

        ChatResponseUpdate? lastUpdate = null;
        var responseText = new System.Text.StringBuilder();
        using var responseActivity = StartActivity("ChatClient.GetStreamingResponseAsync Response");

        ChatResponseUpdate update;

        await using var enumerator = base
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                update = enumerator.Current;
            }
            catch (Exception ex)
            {
                responseActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                responseActivity?.AddEvent(new ActivityEvent("exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message }
                    }));
                throw;
            }

            lastUpdate = update;

            if (string.IsNullOrWhiteSpace(update.Text))
            {
                continue;
            }

            responseText.Append(update.Text);
            yield return update;
        }

        responseActivity?.SetTag("chat.model", lastUpdate?.ModelId);
        responseActivity?.SetTag("chat.response", responseText.ToString());
        responseActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    private Activity? StartActivity(string operationName)
    {
        var parentContext = Activity.Current?.Context;

        var activity = parentContext.HasValue
            ? ActivitySource.StartActivity(operationName, ActivityKind.Internal, parentContext.Value)
            : ActivitySource.StartActivity(operationName, ActivityKind.Internal);

        var provider = config.AiOpenAiApiKey != null ? "OpenAI" : "Ollama";
        activity?.SetTag("chat.provider", provider);
        return activity;
    }

    private string? GetLastUserMessage(IEnumerable<ChatMessage> messages)
    {
        return messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
    }
}
