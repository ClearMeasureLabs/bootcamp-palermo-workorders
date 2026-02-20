using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
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
        _logger.LogDebug("Receiving {messageType}", webServiceMessage.TypeName);
        var bodyObject = webServiceMessage.GetBodyObject();

        if (bodyObject is IRemotableRequest remotableRequest)
        {
            // Server-side validation
            if (remotableRequest is StateCommandBase stateCommand && stateCommand.WorkOrder != null)
            {
                var validationErrors = new List<string>();
                
                if (string.IsNullOrWhiteSpace(stateCommand.WorkOrder.Title))
                {
                    validationErrors.Add("Title is required");
                }
                
                if (string.IsNullOrWhiteSpace(stateCommand.WorkOrder.Description))
                {
                    validationErrors.Add("Description is required");
                }
                
                if (validationErrors.Any())
                {
                    return BadRequest(string.Join("; ", validationErrors));
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

        throw new InvalidOperationException($"Received a message of type {webServiceMessage.TypeName} that is not a request or event");
    }
}