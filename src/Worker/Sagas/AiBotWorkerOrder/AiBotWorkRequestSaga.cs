using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Worker.Sagas.AiBotWorkerOrder.Commands;
using Worker.Sagas.AiBotWorkerOrder.Events;

namespace Worker.Sagas.AiBotWorkerOrder;

public class AiBotWorkRequestSaga(IBus bus, ChatClientFactory chatClientFactory) :
    Saga<AiBotWorkRequestSagaState>,
    IAmStartedByMessages<StartAiBotWorkRequestSagaCommand>,
    IHandleMessages<AiBotStartedWorkRequestEvent>,
    IHandleMessages<AiBotUpdatedWorkerOrderEvent>,
    IHandleMessages<AiBotCompletedWorkRequestEvent>
{

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AiBotWorkRequestSagaState> mapper)
    {
        mapper.MapSaga(state => state.SagaId)
            .ToMessage<StartAiBotWorkRequestSagaCommand>(command => command.SagaId)
            .ToMessage<AiBotStartedWorkRequestEvent>(@event => @event.SagaId)
            .ToMessage<AiBotUpdatedWorkerOrderEvent>(@event => @event.SagaId)
            .ToMessage<AiBotCompletedWorkRequestEvent>(@event => @event.SagaId);
    }

    public async Task Handle(StartAiBotWorkRequestSagaCommand message, IMessageHandlerContext context)
    {
        Data.SagaId = message.SagaId;
        Data.WorkRequestNumber = message.WorkRequestNumber;

        var query = new WorkRequestByNumberQuery(Data.WorkRequestNumber);
        Data.WorkRequest = (await bus.Send(query))!;

        if (Data.WorkRequest?.Assignee is null)
        {
            MarkAsComplete();
            return;
        }

        var command = new AssignedToInProgressCommand(Data.WorkRequest, Data.WorkRequest.Assignee);
        var commandResult = await bus.Send(command);
        Data.WorkRequest = commandResult.WorkRequest;

        var @event = new AiBotStartedWorkRequestEvent(Data.SagaId);
        await context.Publish(@event);
    }

    public async Task Handle(AiBotStartedWorkRequestEvent @event, IMessageHandlerContext context)
    {
        var chatMessages = new List<ChatMessage>()
        {
            new(ChatRole.User, "Hello, world!")
        };

        var chatClient = await chatClientFactory.GetChatClient();
        var chatResponse = await chatClient.GetResponseAsync(chatMessages, cancellationToken: context.CancellationToken);

        Data.WorkRequest.Description = $"{Data.WorkRequest.Description}{Environment.NewLine}{Environment.NewLine}AI Bot: {chatResponse.Messages.Last()}";

        var updatedEvent = new AiBotUpdatedWorkerOrderEvent(Data.SagaId);
        await context.Publish(updatedEvent);
    }

    public async Task Handle(AiBotUpdatedWorkerOrderEvent @event, IMessageHandlerContext context)
    {
        var command = new InProgressToCompleteCommand(Data.WorkRequest, Data.WorkRequest.Assignee!);
        var commandResult = await bus.Send(command);
        Data.WorkRequest = commandResult.WorkRequest;

        var completedEvent = new AiBotCompletedWorkRequestEvent(Data.SagaId);
        await context.Publish(completedEvent);
    }

    public Task Handle(AiBotCompletedWorkRequestEvent @event, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}