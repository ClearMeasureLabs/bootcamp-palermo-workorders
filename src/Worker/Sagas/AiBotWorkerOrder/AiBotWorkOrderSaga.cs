using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Worker.Sagas.AiBotWorkerOrder.Commands;
using Worker.Sagas.AiBotWorkerOrder.Events;

namespace Worker.Sagas.AiBotWorkerOrder;

public class AiBotWorkOrderSaga(IBus bus, ChatClientFactory chatClientFactory) :
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
        Data.SagaId = message.SagaId;
        Data.WorkOrderNumber = message.WorkOrderNumber;

        var query = new WorkOrderByNumberQuery(Data.WorkOrderNumber);
        Data.WorkOrder = (await bus.Send(query))!;

        if (Data.WorkOrder?.Assignee is null)
        {
            MarkAsComplete();
            return;
        }

        var command = new AssignedToInProgressCommand(Data.WorkOrder, Data.WorkOrder.Assignee);
        await bus.Send(command);

        var @event = new AiBotStartedWorkOrderEvent(Data.SagaId);
        await context.Publish(@event);
    }

    public async Task Handle(AiBotStartedWorkOrderEvent @event, IMessageHandlerContext context)
    {
        var chatMessages = new List<ChatMessage>()
        {
            new(ChatRole.User, "Hello, world!")
        };

        var chatClient = await chatClientFactory.GetChatClient();
        var chatResponse = await chatClient.GetResponseAsync(chatMessages, cancellationToken: context.CancellationToken);

        Data.WorkOrder.Description = $"{Data.WorkOrder.Description}{Environment.NewLine}{Environment.NewLine}AI Bot: {chatResponse.Messages.Last()}";
        var command = new UpdateDescriptionCommand(Data.WorkOrder, Data.WorkOrder.Assignee!);
        await bus.Send(command);

        var updatedEvent = new AiBotUpdatedWorkerOrderEvent(Data.SagaId);
        await context.Publish(updatedEvent);
    }

    public async Task Handle(AiBotUpdatedWorkerOrderEvent @event, IMessageHandlerContext context)
    {
        var command = new InProgressToCompleteCommand(Data.WorkOrder, Data.WorkOrder.Assignee!);
        var result = await bus.Send(command);
        Data.WorkOrder = result.WorkOrder;

        var completedEvent = new AiBotCompletedWorkOrderEvent(Data.SagaId);
        await context.Publish(completedEvent);
    }

    public Task Handle(AiBotCompletedWorkOrderEvent @event, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}