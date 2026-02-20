using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;

namespace ClearMeasure.Bootcamp.UI.Client;

public interface IPublisherGateway
{
    Task<WebServiceMessage?> Publish(IRemotableRequest request);

    Task Publish(IRemotableEvent @event);
}