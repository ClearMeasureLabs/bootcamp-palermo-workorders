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
        using var activity = StartActivity("chat.completion");
        activity?.SetTag("chat.prompt", GetLastUserMessage(messages));

        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            activity?.SetTag("chat.model", response.ModelId);
            activity?.SetTag("chat.response", response.Text);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity("chat.completion.streaming");
        activity?.SetTag("chat.prompt", GetLastUserMessage(messages));

        ChatResponseUpdate? lastUpdate = null;
        var responseText = new System.Text.StringBuilder();

        try
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                lastUpdate = update;
                if (update.Text is not null)
                {
                    responseText.Append(update.Text);
                }
                yield return update;
            }

            activity?.SetTag("chat.model", lastUpdate?.ModelId);
            activity?.SetTag("chat.response", responseText.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        finally
        {
            if (activity?.Status == ActivityStatusCode.Unset)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Stream did not complete successfully");
            }
        }
    }

    private static string? GetLastUserMessage(IEnumerable<ChatMessage> messages)
    {
        return messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
    }

    private static Activity? StartActivity(string operationName)
    {
        var parentContext = Activity.Current?.Context;

        var activity = parentContext.HasValue
            ? ActivitySource.StartActivity(operationName, ActivityKind.Internal, parentContext.Value)
            : ActivitySource.StartActivity(operationName, ActivityKind.Internal);

        activity?.SetTag("chat.provider", "openai");
        activity?.SetTag("chat.operation.name", operationName);

        return activity;
    }
}
