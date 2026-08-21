using System.Diagnostics;
using ClearMeasure.Bootcamp.LlmGateway;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class ChatActivityTracingTests
{
    [Test]
    public void RecordException_WhenActivityPresent_SetsErrorStatusAndExceptionEvent()
    {
        using var activity = new Activity("chat-test");
        activity.Start();
        var ex = new InvalidOperationException("boom");

        ChatActivityTracing.RecordException(activity, ex);

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("boom");
        activity.Events.ShouldContain(e =>
            e.Name == "exception"
            && e.Tags.Any(t => t.Key == "exception.type" && (string?)t.Value == typeof(InvalidOperationException).FullName)
            && e.Tags.Any(t => t.Key == "exception.message" && (string?)t.Value == "boom"));
    }

    [Test]
    public void RecordException_WhenActivityNull_DoesNotThrow()
    {
        Should.NotThrow(() =>
            ChatActivityTracing.RecordException(null, new Exception("ignored")));
    }

    [Test]
    public void RecordStreamingCompletion_WhenActivityPresent_SetsTagsAndEvent()
    {
        using var activity = new Activity("chat-stream");
        activity.Start();

        ChatActivityTracing.RecordStreamingCompletion(activity, "gpt-test", "hello world");

        activity.GetTagItem("chat.model").ShouldBe("gpt-test");
        activity.GetTagItem("chat.response").ShouldBe("hello world");
        activity.Events.ShouldContain(e => e.Name == "response.received");
    }

    [Test]
    public void RecordStreamingCompletion_WhenActivityNull_DoesNotThrow()
    {
        Should.NotThrow(() =>
            ChatActivityTracing.RecordStreamingCompletion(null, "model", "text"));
    }
}
