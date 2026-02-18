namespace ClearMeasure.Bootcamp.Core;

public interface IDistributedBus
{
    Task PublishAsync<TEvent>(TEvent? @event, CancellationToken cancellationToken = default);
}