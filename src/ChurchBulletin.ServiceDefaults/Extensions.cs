using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

// ReSharper disable UnusedMethodReturnValue.Global -- Qodana C6 (#9039): fluent
// IHostApplicationBuilder/IServiceCollection extension-method pattern; chained return value is by
// design, not always used.
namespace ChurchBulletin.ServiceDefaults;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Adds a set of default services and configurations including OpenTelemetry instrumentation, health checks, and service discovery.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddSerilogJsonConsole();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.AddHttpClient();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for the application, including logging, metrics, and tracing.
    /// </summary>
    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otelBuilder = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("ChurchBulletin.Application");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("ChurchBulletin.Application")
                    .AddSource("ChurchBulletin.Application.Bus")
                    .AddSource("ChurchBulletin.LlmGateway")
                    .AddSource("NServiceBus.Core")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    });
            });

        builder.AddOpenTelemetryExporters(otelBuilder);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<LocalTelemetryFileWriter>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<LocalTelemetryFileWriter>());
            builder.Services.AddSingleton<ILoggerProvider, LocalTelemetryLoggerProvider>();
        }
    }

    /// <summary>
    /// Adds the exporters whose configuration is present; both may be active together.
    /// APPLICATIONINSIGHTS_CONNECTION_STRING (set on the app in Azure) turns on the Azure Monitor exporter,
    /// which sends traces, metrics and logs to Application Insights. OTEL_EXPORTER_OTLP_ENDPOINT (set by the Aspire
    /// AppHost locally) turns on the OTLP exporter. The service name and other resource attributes come from
    /// OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES.
    /// </summary>
    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder, OpenTelemetryBuilder otelBuilder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            otelBuilder.UseOtlpExporter();
        }

        var useAzureMonitorExporter = !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

        if (useAzureMonitorExporter)
        {
            otelBuilder.UseAzureMonitor();

            // UseAzureMonitor adds the App Service, Container Apps and VM resource detectors after the SDK's own
            // OTEL_RESOURCE_ATTRIBUTES / OTEL_SERVICE_NAME detectors, so the site or container app name would
            // replace service.name. Applying the OTEL_* variables again, last, keeps the name OTEL_SERVICE_NAME sets.
            otelBuilder.ConfigureResource(resource => resource.AddEnvironmentVariableDetector());
        }
    }

    /// <summary>
    /// Adds default health checks to the service.
    /// </summary>
    private static void AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }

    /// <summary>
    /// Maps default endpoints for health checks. The /health endpoint is mapped in all environments.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.MapHealthChecks(HealthEndpointPath);

        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}