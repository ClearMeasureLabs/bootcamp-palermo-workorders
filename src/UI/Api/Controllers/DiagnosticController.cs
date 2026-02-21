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
        var connectionString = databaseConfiguration.GetConnectionString();

        // Determine the provider's connection type and invoke its static
        // ClearAllPools method to discard cached connections.  New requests
        // will open fresh connections that see the current database state.
        System.Type? connectionType = connectionString.StartsWith(
            "Data Source=", StringComparison.OrdinalIgnoreCase)
            ? System.Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite")
            : System.Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient");

        var clearAllPools = connectionType?.GetMethod("ClearAllPools",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        if (clearAllPools != null)
        {
            clearAllPools.Invoke(null, null);
            return Ok("Connection pools cleared");
        }

        return Ok("No pool clearing available for this provider");
    }
}
