using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Same as <see cref="ApiKeyProtectedWebApplicationFactory"/> with feature flags for diagnostics tests.
/// </summary>
public sealed class DiagnosticsApiKeyProtectedWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
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
                ["ApiKeyAuthentication:ValidationKey"] = ApiKeyProtectedWebApplicationFactory.TestApiKey,
                ["FeatureFlags:SampleFeatureA"] = "true",
                ["FeatureFlags:SampleFeatureB"] = "false"
            });
        });
    }
}
