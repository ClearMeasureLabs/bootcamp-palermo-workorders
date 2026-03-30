using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// UI.Server test host with API key validation enabled for <c>/api/*</c> (except public version/time).
/// </summary>
public sealed class ApiKeyProtectedWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    public const string TestApiKey = "integration-test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "true",
                ["ApiKeyAuthentication:ValidationKey"] = TestApiKey
            });
        });
    }
}
