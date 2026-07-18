using ClearMeasure.Bootcamp.Core.Model.Messages;

namespace Worker.Handlers;

/// <summary>
/// Handles <see cref="TracerBulletCommand"/> by replying with a <see cref="TracerBulletReplyMessage"/>.
/// This proves the NServiceBus Send/Reply pipeline works end-to-end between the
/// originating endpoint and the Worker ("WorkOrderProcessing") endpoint.
/// </summary>
public class TracerBulletHandler : IHandleMessages<TracerBulletCommand>
{
    private readonly ILogger<TracerBulletHandler> _logger;

    public TracerBulletHandler(ILogger<TracerBulletHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TracerBulletCommand message, IMessageHandlerContext context)
    {
        _logger.LogInformation("TracerBullet received: {CorrelationId}. Sending reply.", message.CorrelationId);
        await context.Reply(new TracerBulletReplyMessage(message.CorrelationId));
    }
}
