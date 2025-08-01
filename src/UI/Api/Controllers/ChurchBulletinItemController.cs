using Microsoft.AspNetCore.Mvc;
using ProgrammingWithPalermo.ChurchBulletin.Core.Model;
using ProgrammingWithPalermo.ChurchBulletin.Core.Queries;

namespace ProgrammingWithPalermo.ChurchBulletin.UI.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChurchBulletinItemController : ControllerBase
{
    private readonly IChurchBulletinItemByDateHandler _handler;
    private readonly ILogger<WeatherForecastController> _logger;
    public ChurchBulletinItemController(IChurchBulletinItemByDateHandler handler, ILogger<WeatherForecastController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<ChurchBulletinItem> Get()
    {
        _logger.LogError("LogError Get ChurchBulletin");
        _logger.LogCritical("LogCritical Get ChurchBulletin");
        _logger.LogDebug("LogDebug Get ChurchBulletin");
        _logger.LogInformation("LogInformation Get ChurchBulletin");
        _logger.LogTrace("LogTrace Get ChurchBulletin");
        _logger.LogWarning("LogWarning Get ChurchBulletin");

        IEnumerable<ChurchBulletinItem> items = _handler.Handle(
            new ChurchBulletinItemByDateAndTimeQuery(new DateTime(2000, 1,1)));
        return items;
    }
}