using System.Reflection;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.AI;
using Shouldly;
using Worker.Sagas.AiBotWorkerOrder;
using Worker.Sagas.AiBotWorkerOrder.Commands;
using Worker.Sagas.AiBotWorkerOrder.Events;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class AiBotWorkOrderSagaTests
{
    private static readonly PropertyInfo CompletedProperty =
        typeof(NServiceBus.Saga).GetProperty("Completed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException("Saga.Completed property not found.");

    [Test]
    public async Task Handle_Start_WhenAssigneeNull_ShouldMarkCompleteWithoutPublishing()
    {
        var bus = new StubBus();
        bus.SetResponse<WorkOrderByNumberQuery, WorkOrder?>(new WorkOrder { Number = "WO-1", Assignee = null });
        var saga = new AiBotWorkOrderSaga(bus, new StubChatClientFactory(bus))
        {
            Data = new AiBotWorkOrderSagaState()
        };
        var stub = StubMessageHandlerContext.Create();

        await saga.Handle(new StartAiBotWorkOrderSagaCommand(Guid.NewGuid(), "WO-1"), stub.Context);

        IsComplete(saga).ShouldBeTrue();
        stub.PublishedMessages.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_Start_WhenAssigneePresent_ShouldAdvanceAndPublishStarted()
    {
        var sagaId = Guid.NewGuid();
        var assignee = new Employee("bot", "AI", "Bot", "bot@test.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-2",
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned,
            Description = "Fix lights"
        };
        var bus = new StubBus();
        bus.SetResponse<WorkOrderByNumberQuery, WorkOrder?>(workOrder);
        bus.SetResponse<AssignedToInProgressCommand, StateCommandResult>(
            new StateCommandResult(workOrder));
        var saga = new AiBotWorkOrderSaga(bus, new StubChatClientFactory(bus))
        {
            Data = new AiBotWorkOrderSagaState()
        };
        var stub = StubMessageHandlerContext.Create();

        await saga.Handle(new StartAiBotWorkOrderSagaCommand(sagaId, "WO-2"), stub.Context);

        saga.Data.SagaId.ShouldBe(sagaId);
        saga.Data.WorkOrderNumber.ShouldBe("WO-2");
        saga.Data.WorkOrder.ShouldBe(workOrder);
        stub.PublishedMessages.Count.ShouldBe(1);
        stub.PublishedMessages[0].ShouldBeOfType<AiBotStartedWorkOrderEvent>().SagaId.ShouldBe(sagaId);
        IsComplete(saga).ShouldBeFalse();
    }

    [Test]
    public async Task Handle_Started_ShouldAppendAiDescriptionAndPublishUpdated()
    {
        var sagaId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Number = "WO-3",
            Assignee = new Employee("bot", "AI", "Bot", "bot@test.com"),
            Description = "Base"
        };
        var bus = new StubBus();
        var saga = new AiBotWorkOrderSaga(bus, new StubChatClientFactory(bus, "done"))
        {
            Data = new AiBotWorkOrderSagaState
            {
                SagaId = sagaId,
                WorkOrderNumber = "WO-3",
                WorkOrder = workOrder
            }
        };
        var stub = StubMessageHandlerContext.Create();

        await saga.Handle(new AiBotStartedWorkOrderEvent(sagaId), stub.Context);

        saga.Data.WorkOrder.Description.ShouldContain("AI Bot:");
        saga.Data.WorkOrder.Description.ShouldContain("done");
        stub.PublishedMessages.Count.ShouldBe(1);
        stub.PublishedMessages[0].ShouldBeOfType<AiBotUpdatedWorkerOrderEvent>().SagaId.ShouldBe(sagaId);
    }

    [Test]
    public async Task Handle_Updated_ShouldCompleteWorkOrderAndPublishCompleted()
    {
        var sagaId = Guid.NewGuid();
        var assignee = new Employee("bot", "AI", "Bot", "bot@test.com");
        var workOrder = new WorkOrder
        {
            Number = "WO-4",
            Assignee = assignee,
            Status = WorkOrderStatus.InProgress
        };
        var completed = new WorkOrder
        {
            Number = "WO-4",
            Assignee = assignee,
            Status = WorkOrderStatus.Complete
        };
        var bus = new StubBus();
        bus.SetResponse<InProgressToCompleteCommand, StateCommandResult>(
            new StateCommandResult(completed));
        var saga = new AiBotWorkOrderSaga(bus, new StubChatClientFactory(bus))
        {
            Data = new AiBotWorkOrderSagaState
            {
                SagaId = sagaId,
                WorkOrder = workOrder
            }
        };
        var stub = StubMessageHandlerContext.Create();

        await saga.Handle(new AiBotUpdatedWorkerOrderEvent(sagaId), stub.Context);

        saga.Data.WorkOrder.Status.ShouldBe(WorkOrderStatus.Complete);
        stub.PublishedMessages.Count.ShouldBe(1);
        stub.PublishedMessages[0].ShouldBeOfType<AiBotCompletedWorkOrderEvent>().SagaId.ShouldBe(sagaId);
    }

    [Test]
    public async Task Handle_Completed_ShouldMarkSagaComplete()
    {
        var saga = new AiBotWorkOrderSaga(new StubBus(), new StubChatClientFactory(new StubBus()))
        {
            Data = new AiBotWorkOrderSagaState { SagaId = Guid.NewGuid() }
        };
        var stub = StubMessageHandlerContext.Create();

        await saga.Handle(new AiBotCompletedWorkOrderEvent(saga.Data.SagaId), stub.Context);

        IsComplete(saga).ShouldBeTrue();
        stub.PublishedMessages.ShouldBeEmpty();
    }

    private static bool IsComplete(AiBotWorkOrderSaga saga) =>
        (bool)CompletedProperty.GetValue(saga)!;

    private sealed class StubChatClientFactory(IBus bus, string reply = "ai-reply") : ChatClientFactory(bus)
    {
        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new StubChatClient(reply));
    }

    private sealed class StubChatClient(string reply) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, reply)]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class StubBus : IBus
    {
        private readonly Dictionary<Type, object?> _responses = new();

        public void SetResponse<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            _responses[typeof(TRequest)] = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (_responses.TryGetValue(request.GetType(), out var response))
            {
                return Task.FromResult((TResponse)response!);
            }

            throw new InvalidOperationException($"No stub response for {request.GetType().Name}");
        }

        public Task<object?> Send(object request) =>
            throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
