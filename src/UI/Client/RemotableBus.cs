﻿using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Client;

public class RemotableBus(IMediator mediator, IPublisherGateway gateway) : Bus(mediator)
{
    private readonly IMediator _mediator = mediator;

    public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is IRemotableRequest remotableRequest)
        {
            var result = await gateway.Publish(remotableRequest) ?? throw new InvalidOperationException();
            var returnEvent = (TResponse)result.GetBodyObject();
            return returnEvent;
        }

        return await _mediator.Send(request);
    }
}