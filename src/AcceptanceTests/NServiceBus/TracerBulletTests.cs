using ClearMeasure.Bootcamp.Core.Model.Messages;
using ClearMeasure.Bootcamp.IntegrationTests;
using NServiceBus;

namespace ClearMeasure.Bootcamp.AcceptanceTests.NServiceBus;

/// <summary>
/// Tracer bullet acceptance test that proves the full NServiceBus Send/Reply pipeline
/// between the test endpoint ("IntegrationTests") and the Worker endpoint ("WorkOrderProcessing").
///
/// Flow:
///   Test  --Send(TracerBulletCommand)--> SqlServerTransport --> Worker
///   Worker --Reply(TracerBulletReplyMessage)--> SqlServerTransport --> Test
///   Test verifies the reply arrived with the correct correlation ID.
/// </summary>
[TestFixture]
public class TracerBulletTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task TracerBullet_WorkerReceivesCommandAndReplies()
    {
        if (!ServerFixture.WorkerStarted)
        {
            Assert.Ignore("Worker is not running (requires SqlServerTransport). Skipping tracer bullet test.");
        }

        var correlationId = Guid.NewGuid();
        var messageSession = TestHost.GetRequiredService<IMessageSession>(newScope: false);

        // Register the signal before sending so the handler can complete it
        var replyTask = TracerBulletSignal.WaitForReply(correlationId, TimeSpan.FromSeconds(60));

        // Send the command — routed to "WorkOrderProcessing" endpoint via TestHost routing config
        await messageSession.Send(new TracerBulletCommand(correlationId));
        TestContext.Out.WriteLine($"TracerBulletCommand sent with CorrelationId={correlationId}");

        // Wait for the reply from Worker
        await replyTask;
        TestContext.Out.WriteLine($"TracerBulletReplyMessage received for CorrelationId={correlationId}");
    }
}
