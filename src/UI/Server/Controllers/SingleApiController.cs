using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
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
            _logger.LogDebug($"Receiving {webServiceMessage.TypeName}");
            var result = await bus.Send(webServiceMessage.GetBodyObject()) ?? throw new InvalidOperationException();
            _logger.LogDebug($"Returning {result.GetType().Name}");
            return Ok(new WebServiceMessage(result).GetJson());
        }
        catch (WorkOrderValidationException ex)
        {
            _logger.LogWarning(ex, "Work order validation failed");
            return BadRequest(new { errors = ex.ValidationErrors, message = ex.Message });
        }
    }
}