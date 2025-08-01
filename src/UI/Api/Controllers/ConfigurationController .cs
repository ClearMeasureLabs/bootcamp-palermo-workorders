using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Api.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ConfigurationService _configurationService;

    public ConfigurationController()
    {
        _configurationService = new ConfigurationService();
    }

    [HttpGet]
    public ActionResult<ConfigurationModel> Get()
    {
        var configuration = _configurationService.GetConfiguration();
        return Ok(configuration);
    }
}