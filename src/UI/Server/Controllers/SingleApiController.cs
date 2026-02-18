using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[Route(PublisherGateway.ApiRelativeUrl)]
public class SingleApiController(IBus bus, ILogger<SingleApiController>? logger = null)
    : ControllerBase
{
    private readonly ILogger<SingleApiController> _logger = logger ?? new NullLogger<SingleApiController>();

    [HttpPost]
    public async Task<string> Post(WebServiceMessage webServiceMessage)
    {
        _logger.LogDebug("Receiving {messageType}", webServiceMessage.TypeName);
        var bodyObject = webServiceMessage.GetBodyObject();

        if (bodyObject is IRemotableRequest remotableRequest)
        {
            var result = await bus.Send(remotableRequest) ?? throw new InvalidOperationException();
            _logger.LogDebug("Returning {resultType}", result.GetType().Name);
            return new WebServiceMessage(result).GetJson();
        }

        if (bodyObject is IRemotableEvent @event)
        {
            await bus.Publish(@event);
            _logger.LogDebug("Published {eventName}", @event.GetType().Name);
            return new WebServiceMessage().GetJson();
        }

        throw new InvalidOperationException($"Received a message of type {webServiceMessage.TypeName} that is not a request or event");
    }
}