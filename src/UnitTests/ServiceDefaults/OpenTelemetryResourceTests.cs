using Azure.Monitor.OpenTelemetry.AspNetCore;
using ChurchBulletin.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class OpenTelemetryResourceTests
{
    // Points ingestion and Live Metrics at a closed local port so no telemetry leaves the machine.
    private static readonly string OfflineConnectionString =
        $"InstrumentationKey={Guid.Empty};IngestionEndpoint=http://127.0.0.1:9/;LiveEndpoint=http://127.0.0.1:9/";

    [Test]
    public void AddServiceDefaults_WhenOtelServiceNameConfigured_ShouldUseItAsServiceName()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_SERVICE_NAME"] = "ca-test-ui";
        builder.AddServiceDefaults();
        using var host = builder.Build();

        var resource = host.Services.GetRequiredService<TracerProvider>().GetResource();

        ServiceNameOf(resource.Attributes).ShouldBe("ca-test-ui");
    }

    [Test]
    public void AddServiceDefaults_WhenOtelResourceAttributesConfigured_ShouldUseTheirServiceName()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"] = "service.name=ca-agent-ui,deployment.environment=tdd";
        builder.AddServiceDefaults();
        using var host = builder.Build();

        var attributes = host.Services.GetRequiredService<TracerProvider>().GetResource().Attributes.ToList();

        ServiceNameOf(attributes).ShouldBe("ca-agent-ui");
        attributes.ShouldContain(a => a.Key == "deployment.environment" && (string)a.Value == "tdd");
    }

    [TestCase("WEBSITE_SITE_NAME", "app-test-tdd-ui")]
    [TestCase("CONTAINER_APP_NAME", "ca-test-tdd-ui")]
    public void AddServiceDefaults_WhenAzureMonitorRunsOnAzurePlatform_ShouldKeepOtelServiceName(
        string platformVariable, string platformAppName)
    {
        using var platform = EnvironmentVariableScope.Set(
            (platformVariable, platformAppName),
            ("CONTAINER_APP_REPLICA_NAME", "replica-1"),
            ("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", "true"),
            ("APPLICATIONINSIGHTS_SDKSTATS_DISABLED", "true"));
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = OfflineConnectionString;
        builder.Configuration["OTEL_SERVICE_NAME"] = "test-ui";
        builder.AddServiceDefaults();
        builder.Services.Configure<AzureMonitorOptions>(options =>
        {
            options.EnableLiveMetrics = false;
            options.DisableOfflineStorage = true;
        });
        using var host = builder.Build();

        var attributes = host.Services.GetRequiredService<TracerProvider>().GetResource().Attributes.ToList();

        attributes.ShouldContain(a => a.Key == "cloud.provider" && (string)a.Value == "azure");
        ServiceNameOf(attributes).ShouldBe("test-ui");
    }

    private static string? ServiceNameOf(IEnumerable<KeyValuePair<string, object>> attributes) =>
        attributes.Single(a => a.Key == "service.name").Value as string;

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous;

        private EnvironmentVariableScope(Dictionary<string, string?> previous) => _previous = previous;

        public static EnvironmentVariableScope Set(params (string Name, string Value)[] variables)
        {
            var previous = variables.ToDictionary(v => v.Name, v => Environment.GetEnvironmentVariable(v.Name));
            foreach (var (name, value) in variables)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            return new EnvironmentVariableScope(previous);
        }

        public void Dispose()
        {
            foreach (var (name, value) in _previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
