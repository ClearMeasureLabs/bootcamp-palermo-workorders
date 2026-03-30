using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

/// <summary>
/// Hosts UI.Server with CORS enabled and a fixed allowed origin for integration-style HTTP tests.
/// </summary>
public sealed class CorsEnabledApiWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.UseSetting("Cors:Enabled", "true");
        builder.UseSetting("Cors:AllowedOrigins:0", "https://allowed.example");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["Cors:Enabled"] = "true",
                ["Cors:AllowedOrigins:0"] = "https://allowed.example"
            });
        });
    }
}
