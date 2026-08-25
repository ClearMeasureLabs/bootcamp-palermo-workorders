using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Client;

public class RemotableBus(IMediator mediator, IPublisherGateway gateway) : Bus(mediator)
{
    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is IRemotableRequest remotableRequest)
        {
            var result = await gateway.Publish(remotableRequest) ?? throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(result.TypeName))
            {
                throw new InvalidOperationException(
                    "Remoting response missing TypeName; server likely returned a non-WebServiceMessage error body.");
            }

            var returnEvent = (TResponse)result.GetBodyObject();
            return returnEvent;
        }

        return await base.Send(request);
    }

    public override async Task Publish(INotification notification)
    {
        if (notification is IRemotableEvent remotableEvent)
        {
            await gateway.Publish(remotableEvent);
            return;
        }

        await base.Publish(notification);
    }
}