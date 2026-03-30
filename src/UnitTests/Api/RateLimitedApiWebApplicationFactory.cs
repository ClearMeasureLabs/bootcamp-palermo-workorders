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
    private readonly string _sqlConnectionString;

    public RateLimitedApiWebApplicationFactory(string? sqlConnectionString = null)
    {
        _sqlConnectionString = sqlConnectionString ?? WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid appsettings.Development.json (LocalDB) which overrides in-memory SQLite on Linux.
        builder.UseEnvironment("Testing");
        // Host settings are visible to Program.cs before merged app configuration from WebApplicationFactory.
        builder.UseSetting("ConnectionStrings:SqlConnectionString", _sqlConnectionString);
        builder.UseSetting("ApiRateLimiting:Enabled", "true");
        builder.UseSetting("ApiRateLimiting:PermitLimit", "1");
        builder.UseSetting("ApiRateLimiting:WindowSeconds", "60");
        builder.UseSetting("ApiRateLimiting:SegmentsPerWindow", "2");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = _sqlConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiRateLimiting:Enabled"] = "true",
                ["ApiRateLimiting:PermitLimit"] = "1",
                ["ApiRateLimiting:WindowSeconds"] = "60",
                ["ApiRateLimiting:SegmentsPerWindow"] = "2"
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
            });
        });
    }
}
