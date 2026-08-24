using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

// ReSharper disable UnusedMethodReturnValue.Global -- Qodana C6 (#9039): fluent
// IHostApplicationBuilder/IApplicationBuilder extension-method pattern; chained return value is by
// design, not always used.
namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// Shared Serilog setup: compact JSON per line on stdout for container log aggregation.
/// </summary>
public static class SerilogExtensions
{
    private static readonly MethodInfo? HostApplicationBuilderAsHostBuilderMethod = typeof(HostApplicationBuilder)
        .GetMethod("AsHostBuilder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    /// Configures Serilog as the primary logging pipeline with JSON console output and forwards
    /// events to other <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/> registrations
    /// (for example OpenTelemetry and Application Insights).
    /// </summary>
    public static void AddSerilogJsonConsole(this IHostApplicationBuilder builder)
    {
        WireSerilogToHost(builder);
    }

    private static void WireSerilogToHost(IHostApplicationBuilder builder)
    {
        var writeToProviders = ShouldWriteToProviders(builder.Environment);
        switch (builder)
        {
            case WebApplicationBuilder web:
                web.Host.UseSerilog(ConfigureSerilog, writeToProviders: writeToProviders);
                return;
            case HostApplicationBuilder generic when TryGetHostBuilder(generic, out var hostBuilder):
                hostBuilder.UseSerilog(ConfigureSerilog, writeToProviders: writeToProviders);
                return;
            default:
                throw new NotSupportedException(
                    $"Serilog host wiring is not supported for builder type {builder.GetType().FullName}.");
        }
    }

    /// <summary>
    /// Serilog forwards to MEL providers when <c>writeToProviders</c> is true (production).
    /// Test hosts use the <c>Testing</c> environment to avoid duplicate console lines from Serilog JSON plus MEL console.
    /// </summary>
    public static bool ShouldWriteToProviders(IHostEnvironment environment) =>
        !environment.IsEnvironment("Testing");

    private static bool TryGetHostBuilder(HostApplicationBuilder generic, out IHostBuilder hostBuilder)
    {
        if (HostApplicationBuilderAsHostBuilderMethod?.Invoke(generic, null) is IHostBuilder resolved)
        {
            hostBuilder = resolved;
            return true;
        }

        hostBuilder = null!;
        return false;
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
