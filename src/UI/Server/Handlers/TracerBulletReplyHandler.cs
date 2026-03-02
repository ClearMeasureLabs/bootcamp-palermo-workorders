using ClearMeasure.Bootcamp.Core.Model.Messages;

namespace ClearMeasure.Bootcamp.UI.Server.Handlers;

/// <summary>
/// Handles <see cref="TracerBulletReplyMessage"/> replies arriving from the Worker endpoint.
/// Signals the waiting health check via <see cref="TracerBulletSignal"/>.
/// </summary>
public class TracerBulletReplyHandler : IHandleMessages<TracerBulletReplyMessage>
{
    public Task Handle(TracerBulletReplyMessage message, IMessageHandlerContext context)
    {
        TracerBulletSignal.Complete(message.CorrelationId);
        return Task.CompletedTask;
    }
}
