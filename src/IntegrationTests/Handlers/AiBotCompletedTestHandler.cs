using NServiceBus;
using Worker.Sagas.AiBotWorkerOrder.Events;

namespace ClearMeasure.Bootcamp.IntegrationTests.Handlers;

/// <summary>
/// Handles <see cref="AiBotCompletedWorkOrderEvent"/> in the integration test endpoint.
/// Signals the waiting test via <see cref="AiBotSagaSignal"/>.
/// </summary>
public class AiBotCompletedTestHandler : IHandleMessages<AiBotCompletedWorkOrderEvent>
{
    public Task Handle(AiBotCompletedWorkOrderEvent message, IMessageHandlerContext context)
    {
        AiBotSagaSignal.Complete(message.WorkOrderNumber);
        return Task.CompletedTask;
    }
}
