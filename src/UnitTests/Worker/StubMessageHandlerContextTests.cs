using NServiceBus;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class StubMessageHandlerContextTests
{
    [Test]
    public async Task Context_Publish_ShouldRecordPublishedMessage()
    {
        var stub = StubMessageHandlerContext.Create();
        var message = new object();

        await stub.Context.Publish(message, new PublishOptions());

        stub.PublishedMessages.Count.ShouldBe(1);
        stub.PublishedMessages[0].ShouldBeSameAs(message);
    }

    [Test]
    public void Context_MessageIdAndHeaders_ShouldReturnEmptyDefaults()
    {
        var stub = StubMessageHandlerContext.Create();

        stub.Context.MessageId.ShouldBe(string.Empty);
        stub.Context.ReplyToAddress.ShouldBe(string.Empty);
        stub.Context.MessageHeaders.ShouldNotBeNull();
        stub.Context.MessageHeaders.ShouldBeEmpty();
        stub.Context.CancellationToken.ShouldBe(CancellationToken.None);
        stub.Context.Extensions.ShouldNotBeNull();
    }
}
