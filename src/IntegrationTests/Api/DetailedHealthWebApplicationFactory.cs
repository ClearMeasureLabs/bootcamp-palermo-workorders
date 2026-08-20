using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server in-process with SQLite in-memory so CI can exercise <c>/api/*</c> without LocalDB.
/// API key auth is enabled so anonymous access to public leaves (e.g. <c>/api/health</c>) can be asserted.
/// </summary>
public sealed class DetailedHealthWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    /// <summary>Validation key configured for this factory when API key auth is enabled.</summary>
    public const string TestApiKey = "integration-test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid appsettings.Development.json (LocalDB) overriding SQLite on Linux CI.
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
                ["ApiKeyAuthentication:ValidationKey"] = TestApiKey,
                ["ApiRateLimiting:Enabled"] = "true",
                ["ApiRateLimiting:PermitLimit"] = "100",
                ["ApiRateLimiting:WindowSeconds"] = "60",
                ["ApiRateLimiting:SegmentsPerWindow"] = "4",
                ["ApiRateLimiting:QueueLimit"] = "0",
                ["FeatureFlags:SampleFeatureA"] = "false",
                ["FeatureFlags:SampleFeatureB"] = "false"
            });
        });
    }
}
