using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static readonly ActivitySource ApplicationActivitySource = new("ChurchBulletin.Application", "1.0.0");

    /// <summary>
    /// Adds a set of default services and configurations including OpenTelemetry instrumentation, health checks, and service discovery.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

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
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
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
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<LocalTelemetryFileWriter>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<LocalTelemetryFileWriter>());
            builder.Services.AddSingleton<ILoggerProvider, LocalTelemetryLoggerProvider>();
        }

        return builder;
    }

    /// <summary>
    /// Adds OpenTelemetry exporters based on the presence of configuration.
    /// If the OTEL_EXPORTER_OTLP_ENDPOINT environment variable is set, the OTLP exporter will be used.
    /// Otherwise, no exporters will be added.
    /// </summary>
    public static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Adds default health checks to the service.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps default endpoints for health checks. The /health endpoint is mapped in all environments.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);

            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}