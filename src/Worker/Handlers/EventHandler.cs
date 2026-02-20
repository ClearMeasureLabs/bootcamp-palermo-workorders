using ClearMeasure.Bootcamp.Core.Model.Events;
using Worker.Sagas.AiBotWorkerOrder.Commands;

namespace Worker.Handlers;

public class EventHandler : IHandleMessages<WorkOrderAssignedToBotEvent>
{
    public async Task Handle(WorkOrderAssignedToBotEvent @event, IMessageHandlerContext context)
    {
        var command = new StartAiBotWorkOrderSagaCommand(SagaId: Guid.NewGuid(), WorkOrderNumber: @event.WorkOrderNumber);
        await context.SendLocal(command);
    }
}