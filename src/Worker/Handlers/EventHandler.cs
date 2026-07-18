using ClearMeasure.Bootcamp.Core.Model.Events;
using Worker.Sagas.AiBotWorkerOrder.Commands;

namespace Worker.Handlers;

public class EventHandler : IHandleMessages<WorkRequestAssignedToBotEvent>
{
    public async Task Handle(WorkRequestAssignedToBotEvent @event, IMessageHandlerContext context)
    {
        var command = new StartAiBotWorkRequestSagaCommand(SagaId: Guid.NewGuid(), WorkRequestNumber: @event.WorkRequestNumber);
        await context.SendLocal(command);
    }
}