using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Shared Serilog setup: compact JSON per line on stdout for container log aggregation.
/// </summary>
public static class SerilogExtensions
{
    private static readonly MethodInfo? s_hostApplicationBuilderAsHostBuilder = typeof(HostApplicationBuilder)
        .GetMethod("AsHostBuilder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    /// Configures Serilog as the primary logging pipeline with JSON console output and forwards
    /// events to other <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/> registrations
    /// (for example OpenTelemetry and Application Insights).
    /// </summary>
    public static void AddSerilogJsonConsole(this IHostApplicationBuilder builder)
    {
        switch (builder)
        {
            case WebApplicationBuilder web:
                web.Host.UseSerilog(ConfigureSerilog, writeToProviders: true);
                break;
            case HostApplicationBuilder generic:
                if (s_hostApplicationBuilderAsHostBuilder?.Invoke(generic, null) is not IHostBuilder hostBuilder)
                {
                    throw new InvalidOperationException(
                        "Could not obtain IHostBuilder from HostApplicationBuilder for Serilog configuration.");
                }

                hostBuilder.UseSerilog(ConfigureSerilog, writeToProviders: true);
                break;
            default:
                throw new NotSupportedException(
                    $"Serilog host wiring is not supported for builder type {builder.GetType().FullName}.");
        }
    }

    private static void ConfigureSerilog(HostBuilderContext context, IServiceProvider services, LoggerConfiguration lc)
    {
        lc.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .WriteTo.Console(new RenderedCompactJsonFormatter());
    }

    /// <summary>
    /// Flushes Serilog sinks when the web host stops.
    /// </summary>
    public static WebApplication UseSerilogShutdown(this WebApplication app)
    {
        app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        return app;
    }

    /// <summary>
    /// Flushes Serilog sinks when the generic host stops.
    /// </summary>
    public static IHost UseSerilogShutdown(this IHost host)
    {
        host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(Log.CloseAndFlush);
        return host;
    }
}
