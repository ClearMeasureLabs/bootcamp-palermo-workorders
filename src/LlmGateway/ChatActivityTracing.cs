using System.Diagnostics;

namespace ClearMeasure.Bootcamp.LlmGateway;

internal static class ChatActivityTracing
{
    public static void RecordException(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent("exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
    }

    public static void RecordStreamingCompletion(Activity? activity, string? modelId, string responseText)
    {
        activity?.AddEvent(new ActivityEvent("response.received"));
        activity?.SetTag("chat.model", modelId);
        activity?.SetTag("chat.response", responseText);
    }
}
