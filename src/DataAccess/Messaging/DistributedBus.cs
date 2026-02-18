using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.DataAccess.Messaging;

public class DistributedBus(IMessageSession messageSession) : IDistributedBus
{
    public async Task PublishAsync<TEvent>(TEvent? @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            return;
        }

        await messageSession.Publish(@event, cancellationToken);
    }
}
