using ClearMeasure.Bootcamp.Core.Model.Events;

namespace Worker.Handlers;

public class AiBotHandler : IHandleMessages<WorkOrderAssignedToBotEvent>
{
    public Task Handle(WorkOrderAssignedToBotEvent message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
