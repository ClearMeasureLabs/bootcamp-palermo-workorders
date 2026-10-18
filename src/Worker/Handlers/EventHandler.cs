using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.Core.Model.Events;
using Worker.Sagas.AiBotWorkerOrder.Commands;

namespace Worker.Handlers;

public class EventHandler : IHandleMessages<WorkOrderAssignedToBotEvent>
{
    public async Task Handle(WorkOrderAssignedToBotEvent @event, IMessageHandlerContext context)
    {
        var sagaId = new Guid(MD5.HashData(Encoding.UTF8.GetBytes(@event.WorkOrderNumber)));
        var command = new StartAiBotWorkOrderSagaCommand(SagaId: sagaId, WorkOrderNumber: @event.WorkOrderNumber);
        await context.SendLocal(command);
    }
}