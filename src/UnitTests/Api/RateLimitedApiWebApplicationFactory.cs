using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

/// <summary>
/// Hosts UI.Server with a very low API rate limit for exercising 429 responses.
/// </summary>
public sealed class RateLimitedApiWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid appsettings.Development.json (LocalDB) which overrides in-memory SQLite on Linux.
        builder.UseEnvironment("Testing");
        // Host settings are visible to Program.cs before merged app configuration from WebApplicationFactory.
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.UseSetting("ApiRateLimiting:Enabled", "true");
        builder.UseSetting("ApiRateLimiting:PermitLimit", "1");
        builder.UseSetting("ApiRateLimiting:WindowSeconds", "60");
        builder.UseSetting("ApiRateLimiting:SegmentsPerWindow", "2");
        builder.UseSetting("ApiRateLimiting:QueueLimit", "0");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiRateLimiting:Enabled"] = "true",
                ["ApiRateLimiting:PermitLimit"] = "1",
                ["ApiRateLimiting:WindowSeconds"] = "60",
                ["ApiRateLimiting:SegmentsPerWindow"] = "2",
                ["ApiRateLimiting:QueueLimit"] = "0",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<ApiRateLimitingOptions>(o =>
            {
                o.Enabled = true;
                o.PermitLimit = 1;
                o.WindowSeconds = 60;
                o.SegmentsPerWindow = 2;
                o.QueueLimit = 0;
            });
        });
    }
}
