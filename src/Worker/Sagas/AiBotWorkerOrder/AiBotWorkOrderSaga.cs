using Worker.Sagas.AiBotWorkerOrder.Commands;
using Worker.Sagas.AiBotWorkerOrder.Events;

namespace Worker.Sagas.AiBotWorkerOrder;

public class AiBotWorkOrderSaga() :
    Saga<AiBotWorkOrderSagaState>,
    IAmStartedByMessages<StartAiBotWorkOrderSagaCommand>,
    IHandleMessages<AiBotStartedWorkOrderEvent>,
    IHandleMessages<AiBotUpdatedWorkerOrderEvent>,
    IHandleMessages<AiBotCompletedWorkOrderEvent>
{

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AiBotWorkOrderSagaState> mapper)
    {
        mapper.MapSaga(state => state.SagaId)
            .ToMessage<StartAiBotWorkOrderSagaCommand>(command => command.SagaId)
            .ToMessage<AiBotStartedWorkOrderEvent>(@event => @event.SagaId)
            .ToMessage<AiBotUpdatedWorkerOrderEvent>(@event => @event.SagaId)
            .ToMessage<AiBotCompletedWorkOrderEvent>(@event => @event.SagaId);
    }

    public async Task Handle(StartAiBotWorkOrderSagaCommand message, IMessageHandlerContext context)
    {
        Data.WorkOrderNumber = message.WorkOrderNumber;

        //var query = new WorkOrderByNumberQuery(Data.WorkOrderNumber);
        //var workOrder = await bus.Send(query);

        //if (workOrder?.Assignee is null)
        //{
        //    MarkAsComplete();
        //    return;
        //}

        //Data.WorkOrder = workOrder;

        //var command = new AssignedToInProgressCommand(Data.WorkOrder, Data.WorkOrder.Assignee);
        //await bus.Send(command);

        var @event = new AiBotStartedWorkOrderEvent(Data.SagaId);
        await context.Publish(@event);
    }

    public async Task Handle(AiBotStartedWorkOrderEvent @event, IMessageHandlerContext context)
    {
        var updatedEvent = new AiBotUpdatedWorkerOrderEvent(Data.SagaId);
        await context.Publish(updatedEvent);
    }

    public async Task Handle(AiBotUpdatedWorkerOrderEvent @event, IMessageHandlerContext context)
    {
        var completedEvent = new AiBotCompletedWorkOrderEvent(Data.SagaId);
        await context.Publish(completedEvent);
    }

    public Task Handle(AiBotCompletedWorkOrderEvent @event, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}