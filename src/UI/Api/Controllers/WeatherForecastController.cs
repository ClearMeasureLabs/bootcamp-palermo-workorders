using Asp.Versioning;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
[Route($"{ApiRoutes.VersionedApiPrefix}/[controller]")]
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    [HttpGet]
    [OutputCache(PolicyName = OutputCachePolicyNames.WeatherSample)]
    public IEnumerable<WeatherForecast> Get()
    {
        // _logger.LogError("LogError Get Weather");
        // _logger.LogCritical("LogCritical Get Weather");
        logger.LogDebug("LogDebug Get Weather");
        logger.LogInformation("LogInformation Get Weather");
        logger.LogTrace("LogTrace Get Weather");
        // _logger.LogWarning("LogWarning Get Weather");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}