using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server for <c>/api/whoami</c> integration tests with shared SQLite in-memory database.
/// </summary>
public sealed class WhoAmIWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    internal const string SqliteConnectionString = "Data Source=whoami-integration;Mode=Memory;Cache=Shared";

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
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = "",
                ["FeatureFlags:SampleFeatureA"] = "false",
                ["FeatureFlags:SampleFeatureB"] = "false"
            });
        });
    }
}
