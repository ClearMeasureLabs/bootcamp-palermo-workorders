using ClearMeasure.Bootcamp.Core;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[Route("_diagnostics")]
public class DiagnosticController(IDatabaseConfiguration databaseConfiguration) : ControllerBase
{
    [HttpPost("reset-db-connections")]
    public IActionResult ResetDbConnections()
    {
        databaseConfiguration.ResetConnectionPool();
        return Ok("Connection pools cleared");
    }
}
