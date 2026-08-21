using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Worker.Handlers;
using Worker.Sagas.AiBotWorkerOrder.Commands;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class WorkerHandlerTests
{
    [Test]
    public async Task AiBotHandler_Handle_ShouldCompleteWithoutSending()
    {
        var stub = StubMessageHandlerContext.Create();
        var handler = new AiBotHandler();

        await handler.Handle(new WorkOrderAssignedToBotEvent("WO-1", Guid.NewGuid()), stub.Context);

        stub.SentLocalMessages.ShouldBeEmpty();
        stub.RepliedMessages.ShouldBeEmpty();
        stub.PublishedMessages.ShouldBeEmpty();
    }

    [Test]
    public async Task EventHandler_Handle_ShouldSendLocalStartSagaCommand()
    {
        var stub = StubMessageHandlerContext.Create();
        var handler = new global::Worker.Handlers.EventHandler();

        await handler.Handle(new WorkOrderAssignedToBotEvent("WO-42", Guid.NewGuid()), stub.Context);

        stub.SentLocalMessages.Count.ShouldBe(1);
        var command = stub.SentLocalMessages[0].ShouldBeOfType<StartAiBotWorkOrderSagaCommand>();
        command.WorkOrderNumber.ShouldBe("WO-42");
        command.SagaId.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task TracerBulletHandler_Handle_ShouldReplyWithSameCorrelationId()
    {
        var stub = StubMessageHandlerContext.Create();
        var handler = new TracerBulletHandler(NullLogger<TracerBulletHandler>.Instance);
        var correlationId = Guid.NewGuid();

        await handler.Handle(new TracerBulletCommand(correlationId), stub.Context);

        stub.RepliedMessages.Count.ShouldBe(1);
        var reply = stub.RepliedMessages[0].ShouldBeOfType<TracerBulletReplyMessage>();
        reply.CorrelationId.ShouldBe(correlationId);
    }
}
