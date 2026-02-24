using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.McpServer;

public class NullDistributedBus : IDistributedBus
{
    public Task PublishAsync<TEvent>(TEvent? @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
