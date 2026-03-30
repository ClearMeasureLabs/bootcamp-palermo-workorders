using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

/// <summary>
/// Hosts UI.Server with arbitrary <c>ApiRateLimiting:*</c> settings for middleware tests.
/// </summary>
public sealed class TunableApiRateLimitWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    private readonly IReadOnlyDictionary<string, string?> _overrides;

    public TunableApiRateLimitWebApplicationFactory(IReadOnlyDictionary<string, string?> overrides)
    {
        _overrides = overrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.UseSetting("AI_OpenAI_ApiKey", "");
        builder.UseSetting("AI_OpenAI_Url", "");
        builder.UseSetting("AI_OpenAI_Model", "");
        builder.UseSetting("APPLICATIONINSIGHTS_CONNECTION_STRING", "");
        foreach (var kv in _overrides)
            builder.UseSetting(kv.Key, kv.Value ?? "");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var merged = new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            };
            foreach (var kv in _overrides)
                merged[kv.Key] = kv.Value;
            config.AddInMemoryCollection(merged);
        });
    }
}
