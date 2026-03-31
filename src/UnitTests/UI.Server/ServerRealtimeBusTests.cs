using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ServerRealtimeBusTests
{
    [Test]
    public async Task Should_NotCallHub_When_NotificationIsNotRemotableEvent()
    {
        var mediator = new StubMediator();
        var hub = new RecordingRealtimeHub();
        var bus = new ServerRealtimeBus(mediator, hub);

        await bus.Publish(new MediatRUnitTestNotification()).ConfigureAwait(false);

        hub.BroadcastCount.ShouldBe(0);
    }

    [Test]
    public async Task Should_CallHubWithEvent_When_NotificationIsIRemotableEvent()
    {
        var mediator = new StubMediator();
        var hub = new RecordingRealtimeHub();
        var bus = new ServerRealtimeBus(mediator, hub);

        var evt = new UserLoggedInEvent("u1");
        await bus.Publish(evt).ConfigureAwait(false);

        hub.BroadcastCount.ShouldBe(1);
        hub.LastEvent.ShouldBeSameAs(evt);
    }

    private sealed record MediatRUnitTestNotification : INotification;

    private sealed class StubMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingRealtimeHub : IRealtimeNotificationHub
    {
        public int ConnectionCount => 0;

        public int BroadcastCount { get; private set; }

        public IRemotableEvent? LastEvent { get; private set; }

        public Guid Register(System.Net.WebSockets.WebSocket socket) => Guid.Empty;

        public void Remove(Guid id)
        {
        }

        public Task BroadcastRemotableEventAsync(IRemotableEvent @event, CancellationToken cancellationToken = default)
        {
            BroadcastCount++;
            LastEvent = @event;
            return Task.CompletedTask;
        }
    }
}
