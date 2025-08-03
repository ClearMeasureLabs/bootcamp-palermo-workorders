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
        _logger.LogDebug($"Receiving {webServiceMessage.TypeName}");
        var result = await bus.Send(webServiceMessage.GetBodyObject()) ?? throw new InvalidOperationException();
        _logger.LogDebug($"Returning {result.GetType().Name}");
        return new WebServiceMessage(result).GetJson();
    }
}