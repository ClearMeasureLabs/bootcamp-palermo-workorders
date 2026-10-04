using Azure.Monitor.OpenTelemetry.AspNetCore;
using ChurchBulletin.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class TelemetryExporterTests
{
    private static readonly string TestConnectionString = $"InstrumentationKey={Guid.Empty}";

    [Test]
    public void AddServiceDefaults_WhenApplicationInsightsConnectionStringSet_ShouldWireAzureMonitorExporter()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = TestConnectionString;
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = null;

        builder.AddServiceDefaults();

        UsesAzureMonitor(builder.Services).ShouldBeTrue();
        UsesOtlpExporter(builder.Services).ShouldBeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void AddServiceDefaults_WhenApplicationInsightsConnectionStringMissing_ShouldNotWireAzureMonitorExporter(
        string? connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = connectionString;

        builder.AddServiceDefaults();

        UsesAzureMonitor(builder.Services).ShouldBeFalse();
    }

    [Test]
    public void AddServiceDefaults_WhenOnlyLegacyApplicationInsightsSettingSet_ShouldNotWireAzureMonitorExporter()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = null;
        builder.Configuration["ApplicationInsights:ConnectionString"] = TestConnectionString;

        builder.AddServiceDefaults();

        UsesAzureMonitor(builder.Services).ShouldBeFalse();
    }

    [Test]
    public void AddServiceDefaults_WhenOtlpEndpointAndConnectionStringSet_ShouldWireBothExporters()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317";
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = TestConnectionString;

        builder.AddServiceDefaults();

        UsesAzureMonitor(builder.Services).ShouldBeTrue();
        UsesOtlpExporter(builder.Services).ShouldBeTrue();
    }

    private static bool UsesAzureMonitor(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IConfigureOptions<AzureMonitorOptions>));

    private static bool UsesOtlpExporter(IServiceCollection services) =>
        services.Any(d => d.ServiceType.Assembly.GetName().Name == "OpenTelemetry.Exporter.OpenTelemetryProtocol"
            || d.ImplementationType?.Assembly.GetName().Name == "OpenTelemetry.Exporter.OpenTelemetryProtocol");
}
