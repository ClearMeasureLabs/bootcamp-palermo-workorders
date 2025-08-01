using Microsoft.AspNetCore.Mvc;
using ProgrammingWithPalermo.ChurchBulletin.Core;
using ProgrammingWithPalermo.ChurchBulletin.UI.Api.Configuration;

namespace ProgrammingWithPalermo.ChurchBulletin.UI.Api.Controllers;

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