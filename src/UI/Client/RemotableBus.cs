using MediatR;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Client;

public class RemotableBus(IMediator mediator, IPublisherGateway gateway) : Bus(mediator)
{
    private readonly IMediator _mediator = mediator;

    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is IRemotableRequest remotableRequest)
        {
            WebServiceMessage result = await gateway.Publish(remotableRequest) ?? throw new InvalidOperationException();
            TResponse returnEvent = (TResponse)result.GetBodyObject();
            return returnEvent;
        }

        return await _mediator.Send(request);
    }
}