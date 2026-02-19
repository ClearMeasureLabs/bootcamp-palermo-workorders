using ClearMeasure.Bootcamp.Core.Model.Events;

namespace Worker.Handlers;

// TODO: trigger saga, correlation id is created here, so we can use that to correlate the saga steps
public class AiBotHandler : IHandleMessages<WorkOrderAssignedToBotEvent>
{
    public Task Handle(WorkOrderAssignedToBotEvent message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
