using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// In-process UI.Server host with a fixed <see cref="IDetailedHealthReportProvider"/> for stable ETag conditional GET tests.
/// </summary>
public sealed class FixedDetailedHealthWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    internal static readonly DetailedHealthReport FixedReport = new()
    {
        OverallStatus = ComponentHealthStatus.Healthy,
        CheckedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Components =
        [
            new ComponentHealthEntry
            {
                Name = "API",
                Status = ComponentHealthStatus.Healthy,
                DurationMs = 1.0
            }
        ]
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString,
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
            var descriptors = services.Where(d => d.ServiceType == typeof(IDetailedHealthReportProvider)).ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddSingleton<IDetailedHealthReportProvider>(new StubDetailedHealthReportProvider(FixedReport));
        });
    }

    private sealed class StubDetailedHealthReportProvider(DetailedHealthReport report) : IDetailedHealthReportProvider
    {
        public Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }
}
