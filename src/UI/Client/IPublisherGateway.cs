using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Client;

public interface IPublisherGateway
{
    Task<WebServiceMessage?> Publish(IRemotableRequest request);

    Task Publish(IRemotableEvent @event);
}