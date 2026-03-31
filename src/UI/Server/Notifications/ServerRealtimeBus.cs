using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server.Notifications;

/// <summary>
/// After MediatR publishes, pushes <see cref="IRemotableEvent"/> to WebSocket subscribers.
/// </summary>
public sealed class ServerRealtimeBus(IMediator mediator, IRealtimeNotificationHub hub) : Bus(mediator)
{
    public override async Task Publish(INotification notification)
    {
        await base.Publish(notification).ConfigureAwait(false);

        if (notification is not IRemotableEvent remotableEvent)
        {
            return;
        }

        await Task.Run(async () => await hub.BroadcastRemotableEventAsync(remotableEvent).ConfigureAwait(false))
            .ConfigureAwait(false);
    }
}
