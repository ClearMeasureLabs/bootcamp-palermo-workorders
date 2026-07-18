using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Isolated SQLite shared memory for bulk-import HTTP tests (avoids cross-fixture DB sharing).
/// </summary>
public sealed class WorkOrdersBulkImportWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    internal const string SqliteConnectionString = "Data Source=work-orders-bulk-import;Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", SqliteConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = SqliteConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = ""
            });
        });
    }
}
