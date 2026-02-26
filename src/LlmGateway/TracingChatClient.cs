using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// A delegating chat client that adds distributed tracing spans around chat operations.
/// </summary>
public class TracingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    internal static readonly ActivitySource ActivitySource = new("ChurchBulletin.LlmGateway", "1.0.0");

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity("ChatClient.GetResponseAsync");
        activity?.SetTag("chat.prompt", GetLastUserMessage(messages));
        activity?.AddEvent(new ActivityEvent("request.sent"));

        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            activity?.AddEvent(new ActivityEvent("response.received"));
            activity?.SetTag("chat.model", response.ModelId);
            activity?.SetTag("chat.response", response.Text);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception",
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
        using var activity = StartActivity("ChatClient.GetStreamingResponseAsync");
        activity?.SetTag("chat.prompt", GetLastUserMessage(messages));
        activity?.AddEvent(new ActivityEvent("request.sent"));

        ChatResponseUpdate? lastUpdate = null;
        var responseText = new System.Text.StringBuilder();

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
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("exception",
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

        activity?.AddEvent(new ActivityEvent("response.received"));
        activity?.SetTag("chat.model", lastUpdate?.ModelId);
        activity?.SetTag("chat.response", responseText.ToString());
    }

    private Activity? StartActivity(string operationName)
    {
        var parentContext = Activity.Current?.Context;

        var activity = parentContext.HasValue
            ? ActivitySource.StartActivity(operationName, ActivityKind.Internal, parentContext.Value)
            : ActivitySource.StartActivity(operationName, ActivityKind.Internal);

        var provider = "OpenAI";
        activity?.SetTag("chat.provider", provider);
        return activity;
    }

    private string? GetLastUserMessage(IEnumerable<ChatMessage> messages)
    {
        return messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
    }
}
