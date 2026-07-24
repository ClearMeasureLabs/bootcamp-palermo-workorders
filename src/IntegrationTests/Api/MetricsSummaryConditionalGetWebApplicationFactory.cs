using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Hosts UI.Server with fixed time and request metrics for conditional GET integration tests.
/// </summary>
public sealed class MetricsSummaryConditionalGetWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
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
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(
                new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero)));
            services.RemoveAll<IApplicationRequestMetrics>();
            services.AddSingleton<IApplicationRequestMetrics, FrozenApplicationRequestMetrics>();
            services.RemoveAll<IProcessRuntimeMetrics>();
            services.AddSingleton<IProcessRuntimeMetrics, FrozenProcessRuntimeMetrics>();
        });
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FrozenApplicationRequestMetrics : IApplicationRequestMetrics
    {
        public long TotalRequests => 100;

        public void Increment()
        {
        }
    }

    private sealed class FrozenProcessRuntimeMetrics : IProcessRuntimeMetrics
    {
        public long WorkingSetBytes => 52428800;

        public GcCollectionCounts GcCollections => new(10, 5, 2);
    }
}
