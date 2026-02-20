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
    public async Task<IActionResult> Post(WebServiceMessage webServiceMessage)
    {
        try
        {
            _logger.LogDebug("Receiving {messageType}", webServiceMessage.TypeName);
            var bodyObject = webServiceMessage.GetBodyObject();

            if (bodyObject is IRemotableRequest remotableRequest)
            {
                // Validate SaveDraftCommand for required fields
                if (bodyObject is IStateCommand stateCommand)
                {
                    if (string.IsNullOrWhiteSpace(stateCommand.WorkOrder?.Title))
                    {
                        return BadRequest("Title is required");
                    }
                    if (string.IsNullOrWhiteSpace(stateCommand.WorkOrder?.Description))
                    {
                        return BadRequest("Description is required");
                    }
                }

                var result = await bus.Send(remotableRequest) ?? throw new InvalidOperationException();
                _logger.LogDebug("Returning {resultType}", result.GetType().Name);
                return Ok(new WebServiceMessage(result).GetJson());
            }

            if (bodyObject is IRemotableEvent @event)
            {
                await bus.Publish(@event);
                _logger.LogDebug("Published {eventName}", @event.GetType().Name);
                return Ok(new WebServiceMessage().GetJson());
            }

            return BadRequest($"Received a message of type {webServiceMessage.TypeName} that is not a request or event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return BadRequest(ex.Message);
        }
    }
}